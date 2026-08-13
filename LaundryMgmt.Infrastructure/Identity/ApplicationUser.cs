using LaundryMgmt.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LaundryMgmt.Infrastructure.Identity;

/// <summary>
/// Identity user for staff/admin logins (Admin, StoreManager, Staff, DeliveryBoy).
/// Customer-facing login (if you add a customer portal later) should be a
/// separate concern from this internal staff identity table.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    // Refresh token support (simple single-token-per-user model; swap for a
    // RefreshTokens table if you need multi-device revocation history).
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; set; }
}
