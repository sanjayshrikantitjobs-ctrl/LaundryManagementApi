using System.Security.Claims;
using LaundryMgmt.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LaundryMgmt.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Guid? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>
/// Stub notification service. Wire this to Twilio (SMS/WhatsApp) and SendGrid
/// (email); enqueue via Hangfire's BackgroundJob.Enqueue so a slow provider
/// never blocks the request thread.
/// </summary>
public class NotificationService : INotificationService
{
    public Task SendOrderStatusNotificationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // TODO: BackgroundJob.Enque(() => _smsSender.Send(...)) once Hangfire is wired in Program.cs
        return Task.CompletedTask;
    }
}
