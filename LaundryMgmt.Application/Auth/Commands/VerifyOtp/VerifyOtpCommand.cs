using FluentValidation;
using LaundryMgmt.Application.Auth.Commands.Login;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;

namespace LaundryMgmt.Application.Auth.Commands.VerifyOtp;

public record VerifyOtpCommand(string PhoneNumber, string Code) : IRequest<LoginResultDto>;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, LoginResultDto>
{
    private readonly IOtpService _otpService;
    private readonly IIdentityAuthService _identityAuth;
    private readonly ITokenService _tokenService;

    public VerifyOtpCommandHandler(IOtpService otpService, IIdentityAuthService identityAuth, ITokenService tokenService)
    {
        _otpService = otpService;
        _identityAuth = identityAuth;
        _tokenService = tokenService;
    }

    public async Task<LoginResultDto> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var isValid = await _otpService.ValidateAsync(request.PhoneNumber, request.Code, cancellationToken: cancellationToken);
        if (!isValid)
            throw new UnauthorizedAccessException("That code is invalid or has expired.");

        var user = await _identityAuth.ConfirmPhoneNumberAsync(request.PhoneNumber, cancellationToken)
            ?? throw new UnauthorizedAccessException("That code is invalid or has expired.");

        var (accessToken, expiresAtUtc) = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new LoginResultDto(accessToken, refreshToken, expiresAtUtc, user.UserId, user.FullName, user.Role.ToString());
    }
}
