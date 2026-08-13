using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.PickupDeliveries.Queries.GetMyPickupDeliveries;

public record MyPickupDeliveryDto(
    Guid PickupDeliveryId, Guid OrderId, string OrderNumber, string CustomerName,
    string? AddressLine, DeliveryStatus Status, DateTimeOffset? ScheduledAtUtc, DateTimeOffset? CompletedAtUtc);

/// <summary>The logged-in Pickup/Delivery Agent's own assigned orders — resolved via
/// their linked Employee row (Employee.IdentityUserId), never a client-supplied id.</summary>
public record GetMyPickupDeliveriesQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<MyPickupDeliveryDto>>;

public class GetMyPickupDeliveriesQueryHandler : IRequestHandler<GetMyPickupDeliveriesQuery, PaginatedList<MyPickupDeliveryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyPickupDeliveriesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<MyPickupDeliveryDto>> Handle(GetMyPickupDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Not signed in.");
        var isPickup = string.Equals(_currentUser.Role, "PickupAgent", StringComparison.OrdinalIgnoreCase);

        var employeeId = await _db.Employees
            .Where(e => e.IdentityUserId == userId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!employeeId.HasValue)
            return new PaginatedList<MyPickupDeliveryDto>(new List<MyPickupDeliveryDto>(), 0, request.PageNumber, request.PageSize);

        var query = _db.PickupDeliveries
            .Include(p => p.Order).ThenInclude(o => o!.Customer)
            .Include(p => p.Address)
            .Where(p => p.DeliveryBoyEmployeeId == employeeId.Value && p.IsPickup == isPickup);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.ScheduledAtUtc ?? DateTimeOffset.MaxValue)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new MyPickupDeliveryDto(
                p.Id, p.OrderId, p.Order!.OrderNumber, p.Order.Customer!.FullName,
                p.Address != null ? p.Address.Line1 + ", " + p.Address.City : null,
                p.Status, p.ScheduledAtUtc, p.CompletedAtUtc))
            .ToListAsync(cancellationToken);

        return new PaginatedList<MyPickupDeliveryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
