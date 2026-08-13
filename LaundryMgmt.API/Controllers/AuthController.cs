using LaundryMgmt.Application.Auth.Commands.ForgotPassword;
using LaundryMgmt.Application.Auth.Commands.Login;
using LaundryMgmt.Application.Auth.Commands.Register;
using LaundryMgmt.Application.Auth.Commands.ResetPassword;
using LaundryMgmt.Application.Auth.Commands.VerifyOtp;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMgmt.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>Authenticates a staff/admin/delivery-boy/customer user and issues a JWT
    /// access token + refresh token. Used by both the Angular app and the MAUI app.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login(LoginCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>Customer self-registration. Creates an unverified login + linked
    /// Customer record and sends an OTP over WhatsApp; call /verify-otp to activate it.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResultDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterResultDto>> Register(RegisterCommand command)
    {
        var result = await _sender.Send(command);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Confirms the OTP sent to a phone number during registration and, on
    /// success, logs the customer in immediately (same response shape as /login).</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> VerifyOtp(VerifyOtpCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>Starts a password reset: if the username/email matches an account with
    /// a phone number on file, sends (or, in dev mode with no WhatsApp provider
    /// configured, returns directly) a 6-digit reset code.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResultDto>> ForgotPassword(ForgotPasswordCommand command)
    {
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>Confirms the reset code and sets a new password.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }
}
