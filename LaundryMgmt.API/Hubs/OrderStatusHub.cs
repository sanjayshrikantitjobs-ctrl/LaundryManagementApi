using Microsoft.AspNetCore.SignalR;

namespace LaundryMgmt.API.Hubs;

/// <summary>
/// Clients join a group per order (or per role, e.g. "StoreManagers") to
/// receive live updates as AdvanceOrderStatusCommand fires domain events.
/// A MediatR INotificationHandler&lt;OrderStatusChangedEvent&gt; (Application/Infrastructure)
/// calls IHubContext&lt;OrderStatusHub&gt; to push updates here.
/// </summary>
public class OrderStatusHub : Hub
{
    public async Task JoinOrderGroup(string orderId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

    public async Task LeaveOrderGroup(string orderId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");

    /// <summary>Joined by list/board screens (e.g. Angular OrderListComponent) that
    /// want updates for every order rather than one specific order.</summary>
    public async Task JoinDashboardGroup() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
}
