namespace LaundryMgmt.Domain.Enums;

public enum UserRole
{
    Admin = 0,
    StoreManager = 1,
    Staff = 2,
    DeliveryAgent = 3,
    Customer = 4,
    DepartmentHead = 5,
    PickupAgent = 6
}

public enum OrderChannel
{
    WalkIn = 0,
    PickupRequest = 1,
    Express = 2
}

public enum PaymentStatus
{
    Pending = 0,
    Partial = 1,
    Paid = 2,
    Refunded = 3
}

public enum PaymentMode
{
    Cash = 0,
    Card = 1,
    Upi = 2,
    Wallet = 3,
    Online = 4
}

public enum PricingType
{
    PerItem = 0,
    WeightBased = 1
}

public enum MembershipTier
{
    None = 0,
    Silver = 1,
    Gold = 2
}

public enum CustomerStatus
{
    Active = 0,
    Inactive = 1
}

/// <summary>Which stage of the order lifecycle a garment photo was taken at.
/// Kept to just these two (no separate "Processing" type) since this system has
/// no distinct processing-station workflow yet — processing-stage photos, if ever
/// needed, would be photos attached at pickup time showing garment condition.</summary>
public enum GarmentImageType
{
    Pickup = 0,
    Delivery = 1
}

public enum DeliveryStatus
{
    NotScheduled = 0,
    Scheduled = 1,
    OutForPickup = 2,
    PickedUp = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Failed = 6
}

public enum ComplaintType
{
    MissingClothes = 0,
    DamagedClothes = 1,
    Delay = 2,
    WrongDelivery = 3,
    Other = 4
}

public enum ComplaintStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Rejected = 3
}

public enum ContactMessageType
{
    Feedback = 0,
    Complaint = 1,
    Query = 2
}

public enum ContactMessageStatus
{
    Open = 0,
    Resolved = 1
}

public enum MachineStatus
{
    Idle = 0,
    Running = 1,
    UnderMaintenance = 2,
    OutOfService = 3
}
