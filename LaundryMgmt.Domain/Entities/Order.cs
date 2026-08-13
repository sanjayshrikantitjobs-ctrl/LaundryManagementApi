using LaundryMgmt.Domain.Common;
using LaundryMgmt.Domain.Enums;
using LaundryMgmt.Domain.Events;
using LaundryMgmt.Domain.Exceptions;

namespace LaundryMgmt.Domain.Entities;

public class Order : AuditableEntity
{
    // Order lifecycle, in the fixed sequence the business defines.
    private static readonly OrderStatus[] Sequence =
    {
        OrderStatus.New, OrderStatus.Received, OrderStatus.Sorting, OrderStatus.Washing,
        OrderStatus.Drying, OrderStatus.Ironing, OrderStatus.Packing,
        OrderStatus.ReadyForDelivery, OrderStatus.OutForDelivery, OrderStatus.Delivered
    };

    public string OrderNumber { get; set; } = string.Empty; // human-friendly, e.g. ORD-2026-000123
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public OrderChannel Channel { get; set; } = OrderChannel.WalkIn;
    public OrderStatus Status { get; private set; } = OrderStatus.New;
    public bool IsExpress { get; set; }
    public bool IsSameDay { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? PromoCode { get; set; }
    public decimal GstAmount { get; set; }
    public decimal DeliveryCharge { get; set; }
    public decimal TotalAmount { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public decimal AmountPaid { get; set; }

    public DateTimeOffset? PromisedByUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    /// <summary>
    /// Moves the order to the next status in the fixed pipeline, or to
    /// Cancelled from any non-terminal state. Throws if the transition
    /// isn't allowed, so callers (handlers/controllers) don't need to
    /// re-implement the workflow rules.
    /// </summary>
    public OrderStatusHistory AdvanceTo(OrderStatus newStatus, string? changedBy = null)
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new DomainException($"Order {OrderNumber} is already in a terminal state ({Status}).");

        if (newStatus == OrderStatus.Cancelled)
        {
            Status = OrderStatus.Cancelled;
        }
        else
        {
            var currentIndex = Array.IndexOf(Sequence, Status);
            var targetIndex = Array.IndexOf(Sequence, newStatus);
            if (targetIndex != currentIndex + 1)
                throw new DomainException(
                    $"Cannot move order {OrderNumber} from {Status} to {newStatus}; only the next step in the pipeline is allowed.");

            Status = newStatus;
            if (newStatus == OrderStatus.Delivered)
                DeliveredAtUtc = DateTimeOffset.UtcNow;
        }

        var history = new OrderStatusHistory
        {
            OrderId = Id,
            Status = Status,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            ChangedBy = changedBy
        };
        StatusHistory.Add(history);

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, Status));

        return history;
    }

    /// <summary>Admin override: sets the status directly regardless of pipeline order
    /// (staff sometimes need to jump straight to "Ready for Delivery" after a manual
    /// catch-up, or correct a status set in error) — unlike <see cref="AdvanceTo"/>,
    /// this doesn't enforce "next step only", but still records history and still
    /// blocks changes once the order is Delivered.</summary>
    public OrderStatusHistory? SetStatus(OrderStatus newStatus, string? changedBy = null)
    {
        if (Status == OrderStatus.Delivered)
            throw new DomainException($"Order {OrderNumber} is already Delivered and can no longer be changed.");

        if (newStatus == Status)
            return null;

        Status = newStatus;
        if (newStatus == OrderStatus.Delivered)
            DeliveredAtUtc = DateTimeOffset.UtcNow;

        var history = new OrderStatusHistory
        {
            OrderId = Id,
            Status = Status,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            ChangedBy = changedBy
        };
        StatusHistory.Add(history);

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, Status));

        return history;
    }
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid GarmentId { get; set; }
    public Garment? Garment { get; set; }

    public Guid ServiceId { get; set; }
    public Service? Service { get; set; }

    public int Quantity { get; set; } = 1;
    public decimal? WeightKg { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public string? Barcode { get; set; }
    public string? SpecialInstructions { get; set; }

    public ICollection<OrderItemAddOn> AddOns { get; set; } = new List<OrderItemAddOn>();
}

/// <summary>Snapshot of an add-on selected for a specific order line — Name/Price are
/// copied at order-creation time, matching OrderItem's own snapshot pattern, so later
/// edits to the AddOn catalog don't retroactively change historical orders.</summary>
public class OrderItemAddOn : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public Guid AddOnId { get; set; }
    public AddOn? AddOn { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
    public string? ChangedBy { get; set; }
    public string? Notes { get; set; }
}
