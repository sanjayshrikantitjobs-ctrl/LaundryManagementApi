using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LaundryMgmt.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LaundryMgmt.Infrastructure.Identity;

public interface IJwtTokenService
{
    (string accessToken, DateTimeOffset expiresAtUtc) GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public (string accessToken, DateTimeOffset expiresAtUtc) GenerateAccessToken(ApplicationUser user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var minutes = jwtSection.GetValue<int?>("AccessTokenMinutes") ?? 30;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("full_name", user.FullName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Validates an access token ignoring its expiry, so the refresh
    /// endpoint can confirm the token belongs to this app before issuing a new one.</summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty))
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, parameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
