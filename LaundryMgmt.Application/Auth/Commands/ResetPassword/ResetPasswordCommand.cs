using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;

namespace LaundryMgmt.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string UsernameOrEmail, string Code, string NewPassword) : IRequest;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityAuthService _identityAuth;
    private readonly IOtpService _otpService;

    public ResetPasswordCommandHandler(IIdentityAuthService identityAuth, IOtpService otpService)
    {
        _identityAuth = identityAuth;
        _otpService = otpService;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var found = await _identityAuth.FindUserForPasswordResetAsync(request.UsernameOrEmail, cancellationToken);
        if (found is null || string.IsNullOrWhiteSpace(found.Value.PhoneNumber))
            throw new UnauthorizedAccessException("That code is invalid or has expired.");

        var isValid = await _otpService.ValidateAsync(found.Value.PhoneNumber!, request.Code, "PasswordReset", cancellationToken);
        if (!isValid)
            throw new UnauthorizedAccessException("That code is invalid or has expired.");

        var succeeded = await _identityAuth.SetPasswordAsync(found.Value.UserId, request.NewPassword, cancellationToken);
        if (!succeeded)
            throw new InvalidOperationException("Failed to reset password.");
    }
}
