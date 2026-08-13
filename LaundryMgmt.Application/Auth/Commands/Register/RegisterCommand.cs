using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;

namespace LaundryMgmt.Application.Auth.Commands.Register;

/// <summary>Self-registration for the customer mobile/web portal. Creates the login
/// (unverified) and its linked Customer catalog row, then sends an OTP over WhatsApp —
/// the account can't log in until <see cref="VerifyOtp.VerifyOtpCommand"/> succeeds.</summary>
public record RegisterCommand(string FullName, string PhoneNumber, string Email, string Password) : IRequest<RegisterResultDto>;

/// <summary>DevOtpCode is populated only when no real WhatsApp provider is configured
/// (see IWhatsAppSender.DeliversMessages) — a local-dev convenience so registration is
/// completable without a live WhatsApp Business account, never sent once one is wired up.</summary>
public record RegisterResultDto(Guid UserId, string? DevOtpCode);

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResultDto>
{
    private readonly IIdentityAuthService _identityAuth;
    private readonly IApplicationDbContext _db;
    private readonly IOtpService _otpService;

    public RegisterCommandHandler(IIdentityAuthService identityAuth, IApplicationDbContext db, IOtpService otpService)
    {
        _identityAuth = identityAuth;
        _db = db;
        _otpService = otpService;
    }

    public async Task<RegisterResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userId = await _identityAuth.RegisterCustomerAsync(
            request.FullName, request.PhoneNumber, request.Email, request.Password, cancellationToken);

        var customer = new Customer
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            IdentityUserId = userId
        };
        _db.Customers.Add(customer);

        _db.Notifications.Add(new Notification
        {
            RecipientRole = "Admin",
            Type = NotificationType.NewCustomerRegistered,
            Title = "New customer registered",
            Message = $"{request.FullName} ({request.PhoneNumber}) just created an account.",
            EntityId = customer.Id.ToString()
        });

        await _db.SaveChangesAsync(cancellationToken);

        var devOtpCode = await _otpService.GenerateAndSendAsync(request.PhoneNumber, cancellationToken: cancellationToken);

        return new RegisterResultDto(userId, devOtpCode);
    }
}
