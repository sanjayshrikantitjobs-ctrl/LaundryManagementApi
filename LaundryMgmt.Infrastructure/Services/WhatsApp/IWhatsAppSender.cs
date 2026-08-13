namespace LaundryMgmt.Infrastructure.Services.WhatsApp;

/// <summary>
/// Strategy interface for sending a WhatsApp message, matching the spec's
/// requirement to keep the provider swappable (Meta Cloud API, Twilio, Interakt,
/// Gupshup, AISensy — pick one and add an implementation here + a DI registration
/// in DependencyInjection.cs). Selected at startup via configuration ("WhatsApp:Provider").
/// </summary>
public interface IWhatsAppSender
{
    Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>True when this sender actually delivers to the recipient's phone
    /// (e.g. a configured Twilio account). False for the logging-only fallback, where
    /// nobody but the server console ever sees the message — callers use this to decide
    /// whether an OTP needs to be surfaced back through some other channel instead.</summary>
    bool DeliversMessages { get; }
}
