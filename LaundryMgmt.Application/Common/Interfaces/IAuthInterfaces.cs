using LaundryMgmt.Domain.Enums;

namespace LaundryMgmt.Application.Common.Interfaces;

public record AuthenticatedUserDto(
    Guid UserId, string UserName, string Email, string FullName, UserRole Role,
    bool PhoneNumberConfirmed = true, bool IsCustomerActive = true);

/// <summary>
/// Thin abstraction over ASP.NET Core Identity so Application-layer handlers
/// never take a dependency on Microsoft.AspNetCore.Identity directly.
/// Implemented in Infrastructure/Identity/IdentityAuthService.cs.
/// </summary>
public interface IIdentityAuthService
{
    Task<AuthenticatedUserDto?> ValidateCredentialsAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default);

    /// <summary>Creates a Customer-role login with PhoneNumberConfirmed = false; the
    /// account can't log in until <see cref="ConfirmPhoneNumberAsync"/> succeeds.
    /// Throws <see cref="InvalidOperationException"/> if the phone/email is already taken.</summary>
    Task<Guid> RegisterCustomerAsync(string fullName, string phoneNumber, string? email, string password, CancellationToken cancellationToken = default);

    /// <summary>Marks the phone number verified for the most recently registered
    /// unconfirmed user with that number, and returns their login details.</summary>
    Task<AuthenticatedUserDto?> ConfirmPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Looks up a login by username/email for the forgot-password flow —
    /// returns its phone number (the OTP delivery target) without exposing anything
    /// else, or null if no such account exists.</summary>
    Task<(Guid UserId, string? PhoneNumber)?> FindUserForPasswordResetAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

    /// <summary>Sets a new password directly (no OTP/old-password check) — used once
    /// the caller has already proven the reset OTP, or by an admin resetting a
    /// customer's password.</summary>
    Task<bool> SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    (string accessToken, DateTimeOffset expiresAtUtc) GenerateAccessToken(AuthenticatedUserDto user);
    string GenerateRefreshToken();
}

public record UserSummaryDto(
    Guid Id, string UserName, string? Email, string? PhoneNumber, string FullName, UserRole Role, bool IsActive);

/// <summary>Thin abstraction over ASP.NET Core Identity's UserManager for the
/// Admin-only User Management screen — lets Application-layer handlers list/create/
/// update Identity logins for internal staff/agent roles without depending on
/// Microsoft.AspNetCore.Identity directly. Implemented in
/// Infrastructure/Identity/UserManagementService.cs.</summary>
public interface IUserManagementService
{
    Task<(List<UserSummaryDto> Items, int TotalCount)> GetUsersAsync(
        string? search, UserRole? role, bool? isActive, int pageNumber, int pageSize,
        string? sortBy, string? sortDirection, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates an internal staff/agent login (pre-confirmed, no OTP — unlike
    /// customer self-registration). Throws <see cref="InvalidOperationException"/> if
    /// the username is already taken.</summary>
    Task<Guid> CreateUserAsync(
        string fullName, string userName, string? email, string? phoneNumber,
        string password, UserRole role, CancellationToken cancellationToken = default);

    Task UpdateUserAsync(Guid id, string fullName, string? email, string? phoneNumber, CancellationToken cancellationToken = default);

    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(Guid id, UserRole role, CancellationToken cancellationToken = default);
}

/// <summary>Generates and validates short-lived OTP codes, sending them out via
/// whichever <c>IWhatsAppSender</c> is configured (Infrastructure/Services/WhatsApp).</summary>
public interface IOtpService
{
    /// <summary>Returns the plain-text code only when the underlying sender can't
    /// actually deliver it anywhere the user could see it (the logging-only dev
    /// fallback) — null when a real provider is configured, so the code is never
    /// echoed back over the API once WhatsApp delivery is genuinely wired up.</summary>
    Task<string?> GenerateAndSendAsync(string phoneNumber, string purpose = "Registration", CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(string phoneNumber, string code, string purpose = "Registration", CancellationToken cancellationToken = default);
}
