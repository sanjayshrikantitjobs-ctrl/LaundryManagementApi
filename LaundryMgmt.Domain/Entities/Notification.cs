using LaundryMgmt.Domain.Common;

namespace LaundryMgmt.Domain.Entities;

public enum NotificationType
{
    NewOrder = 0,
    OrderUpdated = 1,
    NewCustomerRegistered = 2
}

/// <summary>In-app notification shown in the bell icon. Targets either a shared
/// management inbox (RecipientRole = "Admin", seen by Admin/StoreManager/Staff alike —
/// this system runs a small single-team back office, so a shared feed is simpler than
/// per-admin fan-out) or one specific login (RecipientUserId, e.g. a customer being
/// told their order changed).</summary>
public class Notification : AuditableEntity
{
    public string? RecipientRole { get; set; }
    public Guid? RecipientUserId { get; set; }

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Id of the order/customer/etc. this notification is about, so the
    /// client can navigate straight to it when clicked.</summary>
    public string? EntityId { get; set; }

    public bool IsRead { get; set; }
}
