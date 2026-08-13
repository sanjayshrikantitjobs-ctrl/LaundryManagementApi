using LaundryMgmt.Domain.Common;

namespace LaundryMgmt.Domain.Entities;

public enum BillingCycle
{
    Monthly = 0,
    Quarterly = 1,
    Yearly = 2,
    HalfYearly = 3
}

public enum SubscriptionStatus
{
    Active = 0,
    Paused = 1,
    Cancelled = 2,
    Expired = 3
}

/// <summary>Admin-managed recurring plan catalog (Basic/Standard/Premium/Family-style
/// membership tiers), shown to customers on the Membership page.</summary>
public class SubscriptionPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public int GarmentsPerCycle { get; set; }
    public decimal Price { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SubscriptionPlanFeature> Features { get; set; } = new List<SubscriptionPlanFeature>();
}

/// <summary>A single bullet point shown on a plan's card (e.g. "Free pickup/delivery").</summary>
public class SubscriptionPlanFeature : BaseEntity
{
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

/// <summary>Links a customer to a plan with an admin-set recurring charge (may differ
/// from the plan's list price). No payment/billing automation — admin manages this
/// record by hand.</summary>
public class CustomerSubscription : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public decimal RecurringValue { get; set; }
    public DateOnly StartDate { get; set; }

    /// <summary>Membership validity expiry, computed from the plan's BillingCycle at
    /// assignment time (Monthly=30d, Quarterly=90d, HalfYearly=182d, Yearly=365d).</summary>
    public DateOnly EndDate { get; set; }
    public DateOnly? NextBillingDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public string? Notes { get; set; }
}
