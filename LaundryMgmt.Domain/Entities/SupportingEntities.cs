using LaundryMgmt.Domain.Common;
using LaundryMgmt.Domain.Enums;

namespace LaundryMgmt.Domain.Entities;

public class Invoice : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty; // e.g. INV-2026-000123
    public decimal SubTotal { get; set; }
    public decimal GstAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Payment : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
    public PaymentMode Mode { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionReference { get; set; }
    public DateTimeOffset PaidAtUtc { get; set; }
}

public class InventoryItem : AuditableEntity
{
    public string Name { get; set; } = string.Empty; // Detergent, Hangers, Packing Covers...
    public string Unit { get; set; } = "pcs";         // pcs, ltr, kg
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
}

public class InventoryTransaction : BaseEntity
{
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public bool IsStockIn { get; set; } // true = purchase/stock-in, false = stock-out/usage
    public decimal Quantity { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset TransactionAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class Employee : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Shift { get; set; }
    public decimal Salary { get; set; }
    public DateOnly JoinedOn { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Links this staff record to the Identity login the employee signs in
    /// with, mirroring <see cref="Customer.IdentityUserId"/>. Set for roles that need
    /// to resolve "which employee is the currently logged-in user" (e.g. Pickup/Delivery
    /// Agent assignment lookups). Null for employees with no portal login.</summary>
    public Guid? IdentityUserId { get; set; }
}

public class Machine : AuditableEntity
{
    public string Name { get; set; } = string.Empty;   // Washer #1, Dryer #2...
    public string Type { get; set; } = string.Empty;   // Washer, Dryer, Iron
    public decimal CapacityKg { get; set; }
    public MachineStatus Status { get; set; } = MachineStatus.Idle;
    public DateOnly? LastMaintenanceOn { get; set; }
    public DateOnly? NextMaintenanceDue { get; set; }
}

public class Complaint : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ComplaintType Type { get; set; }
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public string Description { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public decimal? CompensationAmount { get; set; }
}

/// <summary>A customer-submitted "Contact Us" message (feedback, complaint, or query),
/// distinct from <see cref="Complaint"/> which always ties to a specific order.</summary>
public class ContactMessage : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ContactMessageType Type { get; set; }
    public ContactMessageStatus Status { get; set; } = ContactMessageStatus.Open;
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Response { get; set; }
}

public class PickupDelivery : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public bool IsPickup { get; set; } // true = pickup, false = delivery

    /// <summary>The customer's own address this pickup/delivery is scheduled against
    /// (chosen at checkout, e.g. from their saved address book).</summary>
    public Guid? AddressId { get; set; }
    public CustomerAddress? Address { get; set; }

    public Guid? DeliveryBoyEmployeeId { get; set; }
    public Employee? DeliveryBoy { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.NotScheduled;
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? Otp { get; set; }
    public decimal Charge { get; set; }
}

/// <summary>A garment photo taken during pickup or delivery, for dispute-prevention
/// and operational transparency (which garments, condition, count). Categorized by
/// Service (the existing catalog category) rather than a hardcoded enum, per the
/// existing Service/Garment structure. Metadata-only row — the actual file is written
/// via the existing UploadsController/local disk storage, this just records who
/// uploaded what, when, and for which order/service/item.</summary>
public class OrderGarmentImage : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>The Service category this photo belongs to (e.g. Dry Cleaning,
    /// Ironing) — optional since a pickup/delivery-proof photo may cover the whole
    /// order rather than one specific service line.</summary>
    public Guid? ServiceId { get; set; }
    public Service? Service { get; set; }

    public Guid? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public GarmentImageType ImageType { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string? Notes { get; set; }
}
