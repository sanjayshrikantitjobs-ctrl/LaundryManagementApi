using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.OrderGarmentImages.Queries.GetOrderGarmentImages;

public record OrderGarmentImageDto(
    Guid Id, Guid OrderId, Guid? ServiceId, string? ServiceName, Guid? OrderItemId,
    GarmentImageType ImageType, string ImageUrl, string UploadedByName, DateTimeOffset UploadedAtUtc, string? Notes);

/// <summary>Images for one order. Ownership is enforced here, not just by hiding UI:
/// a Customer may only fetch their own order's images; a Pickup/Delivery Agent only
/// an order actually assigned to them; management roles may fetch any order.</summary>
public record GetOrderGarmentImagesQuery(Guid OrderId) : IRequest<List<OrderGarmentImageDto>>;

public class GetOrderGarmentImagesQueryHandler : IRequestHandler<GetOrderGarmentImagesQuery, List<OrderGarmentImageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrderGarmentImagesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<OrderGarmentImageDto>> Handle(GetOrderGarmentImagesQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found.");

        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Not signed in.");
        var role = _currentUser.Role;

        if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            if (order.Customer?.IdentityUserId != userId)
                throw new UnauthorizedAccessException("You can only view your own order's images.");
        }
        else if (string.Equals(role, "PickupAgent", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(role, "DeliveryAgent", StringComparison.OrdinalIgnoreCase))
        {
            var isPickup = string.Equals(role, "PickupAgent", StringComparison.OrdinalIgnoreCase);
            var employeeId = await _db.Employees
                .Where(e => e.IdentityUserId == userId)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var isAssigned = employeeId.HasValue && await _db.PickupDeliveries.AnyAsync(
                p => p.OrderId == request.OrderId && p.IsPickup == isPickup && p.DeliveryBoyEmployeeId == employeeId.Value,
                cancellationToken);

            if (!isAssigned)
                throw new UnauthorizedAccessException("This order isn't assigned to you.");
        }
        // Admin/StoreManager/Staff/DepartmentHead: full visibility, no extra check.

        return await _db.OrderGarmentImages
            .Where(i => i.OrderId == request.OrderId)
            .OrderBy(i => i.UploadedAtUtc)
            .Select(i => new OrderGarmentImageDto(
                i.Id, i.OrderId, i.ServiceId, i.Service != null ? i.Service.Name : null, i.OrderItemId,
                i.ImageType, i.ImageUrl, i.UploadedByName, i.UploadedAtUtc, i.Notes))
            .ToListAsync(cancellationToken);
    }
}
