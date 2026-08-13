using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;

namespace LaundryMgmt.Application.Auth.Commands.ForgotPassword;

/// <summary>DevOtpCode mirrors RegisterResultDto's dev fallback — populated only when
/// no real WhatsApp provider is configured. AccountFound is deliberately returned
/// (rather than always responding identically) since this is a single-tenant dev/demo
/// system where the reset flow needs to actually be usable without email delivery.</summary>
public record ForgotPasswordResultDto(bool AccountFound, string? DevOtpCode);

public record ForgotPasswordCommand(string UsernameOrEmail) : IRequest<ForgotPasswordResultDto>;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty();
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResultDto>
{
    private readonly IIdentityAuthService _identityAuth;
    private readonly IOtpService _otpService;

    public ForgotPasswordCommandHandler(IIdentityAuthService identityAuth, IOtpService otpService)
    {
        _identityAuth = identityAuth;
        _otpService = otpService;
    }

    public async Task<ForgotPasswordResultDto> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var found = await _identityAuth.FindUserForPasswordResetAsync(request.UsernameOrEmail, cancellationToken);
        if (found is null || string.IsNullOrWhiteSpace(found.Value.PhoneNumber))
            return new ForgotPasswordResultDto(false, null);

        var devOtpCode = await _otpService.GenerateAndSendAsync(found.Value.PhoneNumber!, "PasswordReset", cancellationToken);
        return new ForgotPasswordResultDto(true, devOtpCode);
    }
}
