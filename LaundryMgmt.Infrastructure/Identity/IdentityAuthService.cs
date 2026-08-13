using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Infrastructure.Identity;

public class IdentityAuthService : IIdentityAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IApplicationDbContext _db;

    public IdentityAuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    public async Task<AuthenticatedUserDto?> ValidateCredentialsAsync(
        string usernameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(usernameOrEmail)
                   ?? await _userManager.FindByEmailAsync(usernameOrEmail);

        if (user is null || !user.IsActive)
            return null;

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
            return null;

        var isCustomerActive = true;
        if (user.Role == UserRole.Customer)
        {
            var customer = await _db.Customers
                .Where(c => c.IdentityUserId == user.Id)
                .Select(c => (CustomerStatus?)c.Status)
                .FirstOrDefaultAsync(cancellationToken);
            isCustomerActive = customer != CustomerStatus.Inactive;
        }

        return ToDto(user, isCustomerActive);
    }

    public async Task<Guid> RegisterCustomerAsync(
        string fullName, string phoneNumber, string? email, string password, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByNameAsync(phoneNumber) is not null)
            throw new InvalidOperationException($"An account with phone number {phoneNumber} already exists.");

        var user = new ApplicationUser
        {
            UserName = phoneNumber,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = false,
            Email = email,
            EmailConfirmed = false,
            FullName = fullName,
            Role = UserRole.Customer,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, UserRole.Customer.ToString());
        return user.Id;
    }

    public async Task<AuthenticatedUserDto?> ConfirmPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(phoneNumber);
        if (user is null) return null;

        user.PhoneNumberConfirmed = true;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? ToDto(user) : null;
    }

    public async Task<(Guid UserId, string? PhoneNumber)?> FindUserForPasswordResetAsync(
        string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(usernameOrEmail)
                   ?? await _userManager.FindByEmailAsync(usernameOrEmail);
        return user is null ? null : (user.Id, user.PhoneNumber);
    }

    public async Task<bool> SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    private static AuthenticatedUserDto ToDto(ApplicationUser user, bool isCustomerActive = true) => new(
        user.Id, user.UserName ?? user.Email ?? string.Empty,
        user.Email ?? string.Empty, user.FullName, user.Role, user.PhoneNumberConfirmed, isCustomerActive);
}

/// <summary>Adapts the concrete JwtTokenService (which speaks ApplicationUser)
/// to the Application-layer's ITokenService (which speaks AuthenticatedUserDto),
/// so Application never references Microsoft.AspNetCore.Identity.</summary>
public class TokenServiceAdapter : ITokenService
{
    private readonly IJwtTokenService _jwtTokenService;

    public TokenServiceAdapter(IJwtTokenService jwtTokenService) => _jwtTokenService = jwtTokenService;

    public (string accessToken, DateTimeOffset expiresAtUtc) GenerateAccessToken(AuthenticatedUserDto user)
    {
        var pseudoUser = new ApplicationUser
        {
            Id = user.UserId,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
        return _jwtTokenService.GenerateAccessToken(pseudoUser);
    }

    public string GenerateRefreshToken() => _jwtTokenService.GenerateRefreshToken();
}
