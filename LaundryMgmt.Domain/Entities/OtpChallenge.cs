using LaundryMgmt.Domain.Common;

namespace LaundryMgmt.Domain.Entities;

/// <summary>
/// A short-lived one-time code sent to a phone number (via WhatsApp) to verify
/// ownership before an action completes — today just registration, but the
/// Purpose field leaves room for e.g. delivery confirmation later.
/// </summary>
public class OtpChallenge : BaseEntity
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Registration";
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsUsed { get; set; }
    public int AttemptCount { get; set; }
}
