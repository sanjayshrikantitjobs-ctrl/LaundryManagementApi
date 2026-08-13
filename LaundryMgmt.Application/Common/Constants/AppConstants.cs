namespace LaundryMgmt.Application.Common.Constants;

public static class AppConstants
{
    /// <summary>Max garment images allowed per order, per image type (Pickup/Delivery).
    /// Single source of truth so the limit can be changed later without touching the
    /// database schema or any other code.</summary>
    public const int MaxImagesPerOrderCategory = 10;
}
