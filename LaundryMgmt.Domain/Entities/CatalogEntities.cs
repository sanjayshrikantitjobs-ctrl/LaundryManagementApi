using LaundryMgmt.Domain.Common;
using LaundryMgmt.Domain.Enums;

namespace LaundryMgmt.Domain.Entities;

/// <summary>Top-level grouping shown to customers (Wash &amp; Laundry, Ironing &amp; Pressing,
/// Dry Cleaning, ...). Admin-managed; Services are assigned to exactly one category.</summary>
public class ServiceCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconOrImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Service> Services { get; set; } = new List<Service>();
}

public class Garment : AuditableEntity
{
    public string Name { get; set; } = string.Empty;        // Shirt, Pant, Saree...
    public string? SpecialInstructions { get; set; }
    public string? ImageUrl { get; set; }

    public Guid CategoryId { get; set; }
    public ServiceCategory? Category { get; set; }

    public ICollection<GarmentServicePrice> ServicePrices { get; set; } = new List<GarmentServicePrice>();
}

public class Service : AuditableEntity
{
    public string Name { get; set; } = string.Empty;        // Steam Iron, Express Ironing, Dry Clean...

    public Guid CategoryId { get; set; }
    public ServiceCategory? Category { get; set; }

    public decimal BasePrice { get; set; }
    public int EstimatedTimeHours { get; set; }
    public decimal GstPercentage { get; set; }
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Extra flat charge added per item when a customer picks the Express
    /// channel for an order containing this service (admin-configurable) — used as
    /// the fallback when a GarmentServicePrice row doesn't set its own ExpressPrice.</summary>
    public decimal ExpressSurcharge { get; set; }

    /// <summary>Turnaround time (hours) promised when Express is selected — distinct
    /// from EstimatedTimeHours, which is the normal-channel turnaround.</summary>
    public int ExpressEtaHours { get; set; }

    public ICollection<GarmentServicePrice> GarmentPrices { get; set; } = new List<GarmentServicePrice>();
}

/// <summary>Price for a specific Garment + Service combination (e.g. Cotton Shirt + Steam
/// Iron = 30). The four nullable fields below override the Service's own defaults for this
/// specific item; when null, callers fall back to the Service's value.</summary>
public class GarmentServicePrice : BaseEntity
{
    public Guid GarmentId { get; set; }
    public Garment? Garment { get; set; }

    public Guid ServiceId { get; set; }
    public Service? Service { get; set; }

    public PricingType PricingType { get; set; } = PricingType.PerItem;
    public decimal Price { get; set; }          // per item or per kg depending on PricingType

    /// <summary>Full express price for this item (not a delta) — null falls back to
    /// Price + Service.ExpressSurcharge.</summary>
    public decimal? ExpressPrice { get; set; }

    /// <summary>Null falls back to Service.GstPercentage.</summary>
    public decimal? GstPercentage { get; set; }

    /// <summary>Null falls back to Service.EstimatedTimeHours.</summary>
    public int? EstimatedTimeHours { get; set; }

    /// <summary>Null falls back to Service.ExpressEtaHours.</summary>
    public int? ExpressEtaHours { get; set; }

    /// <summary>Hides this specific item+service pairing from customers without
    /// deleting it (independent of the Garment/Service's own soft-delete).</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Selectable, priced add-on (Stain Removal, Premium Packaging, Express
/// Processing) attachable to individual order items.</summary>
public class AddOn : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
