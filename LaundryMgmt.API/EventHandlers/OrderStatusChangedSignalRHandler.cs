using LaundryMgmt.API.Hubs;
using LaundryMgmt.Domain.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace LaundryMgmt.API.EventHandlers;

/// <summary>
/// Lives in the API project (not Application/Infrastructure) because it needs
/// IHubContext&lt;OrderStatusHub&gt;, and the Hub itself is an API-layer concept.
/// Registered via the extra MediatR assembly scan in Program.cs.
/// </summary>
public class OrderStatusChangedSignalRHandler : INotificationHandler<OrderStatusChangedEvent>
{
    private readonly IHubContext<OrderStatusHub> _hubContext;

    public OrderStatusChangedSignalRHandler(IHubContext<OrderStatusHub> hubContext) => _hubContext = hubContext;

    public async Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        // Single payload object — matches Angular's OrderStatusUpdate interface
        // and should be mirrored by a matching record on the MAUI side.
        var payload = new
        {
            orderId = notification.OrderId,
            orderNumber = notification.OrderNumber,
            newStatus = notification.NewStatus
        };

        // Everyone watching this specific order (customer-facing tracking page, if added later).
        await _hubContext.Clients.Group($"order-{notification.OrderId}")
            .SendAsync("OrderStatusChanged", payload, cancellationToken: cancellationToken);

        // General "dashboard" group so order-list/board screens update without
        // joining every individual order's group.
        await _hubContext.Clients.Group("dashboard")
            .SendAsync("OrderStatusChanged", payload, cancellationToken: cancellationToken);
    }
}
