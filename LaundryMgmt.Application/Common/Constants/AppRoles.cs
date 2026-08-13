namespace LaundryMgmt.Application.Common.Constants;

/// <summary>Shared role-group constants for <c>[Authorize(Roles = ...)]</c>, replacing
/// the per-controller duplicated "Admin,StoreManager,Staff" strings. Authorization must
/// always be enforced here (backend), never by hiding UI elements alone.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";

    /// <summary>Full read/write access to master data (pricing, garments, services,
    /// promotions, customers, users) and all order operations.</summary>
    public const string ManagementRoles = "Admin,StoreManager,Staff";

    /// <summary>Management plus Department Head — everyone who can view master data
    /// and orders, but NOT necessarily create/edit/delete master records. Endpoints
    /// gated by this constant must be read-only, or must apply an additional
    /// operational-vs-master-data distinction themselves.</summary>
    public const string OperationalViewRoles = "Admin,StoreManager,Staff,DepartmentHead";

    /// <summary>Management plus Department Head, for order-processing actions
    /// (status updates, garment-image workflow) that a Department Head is allowed to
    /// perform even though they can't touch master pricing/garment/service/promotion data.</summary>
    public const string OperationalRoles = "Admin,StoreManager,Staff,DepartmentHead";

    /// <summary>Agents who fulfil pickups, plus management for oversight.</summary>
    public const string PickupRoles = "Admin,StoreManager,PickupAgent";

    /// <summary>Agents who fulfil deliveries, plus management for oversight.</summary>
    public const string DeliveryRoles = "Admin,StoreManager,DeliveryAgent";

    /// <summary>Everyone allowed to upload an image anywhere in the system: master-data
    /// photos (management/Department Head) and order garment photos (Pickup/Delivery
    /// agents). Individual endpoints still enforce their own ownership/type rules on
    /// top of this — this only gates "can reach the upload endpoint at all".</summary>
    public const string ImageUploadRoles = "Admin,StoreManager,Staff,DepartmentHead,PickupAgent,DeliveryAgent";
}
