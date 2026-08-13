using Microsoft.Extensions.Logging;

namespace LaundryMgmt.Infrastructure.Services.WhatsApp;

/// <summary>
/// Default provider: doesn't call any external API, just logs the message that
/// would have been sent. Safe for local dev/testing without any WhatsApp Business
/// account — the OTP shows up in the server console/log output. Switch to a real
/// provider (see TwilioWhatsAppSender) once you have API credentials.
/// </summary>
public class LoggingWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<LoggingWhatsAppSender> _logger;

    public LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger) => _logger = logger;

    public bool DeliversMessages => false;

    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[WhatsApp:Logging] Would send to {PhoneNumber}: {Message}", toPhoneNumber, message);
        return Task.CompletedTask;
    }
}
