using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LaundryMgmt.Infrastructure.Services.WhatsApp;

/// <summary>
/// Sends via Twilio's WhatsApp API (https://www.twilio.com/docs/whatsapp/api).
/// Needs real credentials in configuration before it does anything — see appsettings
/// "WhatsApp:Twilio" section. Note this does NOT send from an arbitrary personal
/// WhatsApp number: Twilio's free Sandbox sends FROM Twilio's own sandbox number
/// (default +1 415 523 8886), and each recipient must first join the sandbox by
/// WhatsApping the join code Twilio gives you from their own phone. To send from
/// your own business number you need Twilio's WhatsApp Business Platform approval
/// process (business verification through Meta) — that's a real account-setup step
/// outside what this code can do for you.
/// </summary>
public class TwilioWhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioWhatsAppSender> _logger;

    public TwilioWhatsAppSender(HttpClient http, IConfiguration configuration, ILogger<TwilioWhatsAppSender> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Reflects configuration presence, not actual per-call delivery success —
    /// good enough for the OTP dev-fallback decision, which only needs "is anyone
    /// plausibly receiving these" rather than a live health check.</summary>
    public bool DeliversMessages
    {
        get
        {
            var section = _configuration.GetSection("WhatsApp:Twilio");
            return !string.IsNullOrWhiteSpace(section["AccountSid"])
                && !string.IsNullOrWhiteSpace(section["AuthToken"])
                && !string.IsNullOrWhiteSpace(section["FromNumber"]);
        }
    }

    public async Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("WhatsApp:Twilio");
        var accountSid = section["AccountSid"];
        var authToken = section["AuthToken"];
        var fromNumber = section["FromNumber"]; // e.g. "+14155238886" for the Sandbox

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(fromNumber))
        {
            _logger.LogWarning(
                "WhatsApp:Twilio is not configured (AccountSid/AuthToken/FromNumber) — skipping send to {PhoneNumber}. " +
                "Set these via user-secrets once you have a Twilio account.", toPhoneNumber);
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = $"whatsapp:{fromNumber}",
                ["To"] = $"whatsapp:{toPhoneNumber}",
                ["Body"] = message
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}")));

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Twilio WhatsApp send failed ({StatusCode}): {Body}", response.StatusCode, body);
        }
    }
}
