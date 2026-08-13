namespace LaundryMgmt.Shared.Auth;

public record LoginRequest(string UsernameOrEmail, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    string UserId,
    string FullName,
    string Role);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record RegisterRequest(string FullName, string PhoneNumber, string Email, string Password);

public record VerifyOtpRequest(string PhoneNumber, string Code);
