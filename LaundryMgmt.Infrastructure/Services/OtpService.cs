using System.Security.Cryptography;
using System.Text;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Infrastructure.Services.WhatsApp;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Infrastructure.Services;

public class OtpService : IOtpService
{
    private const int CodeLength = 6;
    private const int ExpiryMinutes = 10;
    private const int MaxAttempts = 5;

    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly IDateTimeProvider _dateTime;

    public OtpService(IApplicationDbContext db, IWhatsAppSender whatsAppSender, IDateTimeProvider dateTime)
    {
        _db = db;
        _whatsAppSender = whatsAppSender;
        _dateTime = dateTime;
    }

    public async Task<string?> GenerateAndSendAsync(string phoneNumber, string purpose = "Registration", CancellationToken cancellationToken = default)
    {
        var code = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, CodeLength)).ToString($"D{CodeLength}");

        _db.OtpChallenges.Add(new OtpChallenge
        {
            PhoneNumber = phoneNumber,
            Purpose = purpose,
            CodeHash = Hash(code),
            ExpiresAtUtc = _dateTime.UtcNow.AddMinutes(ExpiryMinutes)
        });
        await _db.SaveChangesAsync(cancellationToken);

        await _whatsAppSender.SendAsync(
            phoneNumber,
            $"Your Laundry Management System verification code is {code}. It expires in {ExpiryMinutes} minutes.",
            cancellationToken);

        return _whatsAppSender.DeliversMessages ? null : code;
    }

    public async Task<bool> ValidateAsync(string phoneNumber, string code, string purpose = "Registration", CancellationToken cancellationToken = default)
    {
        var challenge = await _db.OtpChallenges
            .Where(o => o.PhoneNumber == phoneNumber && o.Purpose == purpose && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null || challenge.ExpiresAtUtc < _dateTime.UtcNow || challenge.AttemptCount >= MaxAttempts)
            return false;

        challenge.AttemptCount++;

        if (challenge.CodeHash != Hash(code))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        challenge.IsUsed = true;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
