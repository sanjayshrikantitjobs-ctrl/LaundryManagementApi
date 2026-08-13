using LaundryMgmt.Domain.Common;
using LaundryMgmt.Domain.Enums;

namespace LaundryMgmt.Domain.Events;

public sealed class OrderStatusChangedEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public OrderStatus NewStatus { get; }
    public DateTimeOffset OccurredOnUtc { get; }

    public OrderStatusChangedEvent(Guid orderId, string orderNumber, OrderStatus newStatus)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        NewStatus = newStatus;
        OccurredOnUtc = DateTimeOffset.UtcNow;
    }
}
