using LaundryMgmt.Domain.Common;

namespace LaundryMgmt.Domain.Entities;

/// <summary>Admin-managed promotion/offer shown to customers (with an image) on the
/// Promotions & Offers page, optionally redeemable via a promo code at checkout.</summary>
public class Promotion : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Code { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}
