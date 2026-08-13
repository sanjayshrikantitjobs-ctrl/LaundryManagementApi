using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.PickupDeliveries.Queries.GetOrderPickupDeliveries;

public record OrderPickupDeliveryDto(
    Guid Id, bool IsPickup, DeliveryStatus Status, DateTimeOffset? ScheduledAtUtc, DateTimeOffset? CompletedAtUtc,
    Guid? AssignedEmployeeId, string? AssignedEmployeeName);

/// <summary>The pickup and/or delivery legs for one order — used by the order-detail
/// screen to show scheduling/agent-assignment status. Management-only (see the
/// controller); handler still resolves data straight from the OrderId, no
/// customer-facing exposure needed here.</summary>
public record GetOrderPickupDeliveriesQuery(Guid OrderId) : IRequest<List<OrderPickupDeliveryDto>>;

public class GetOrderPickupDeliveriesQueryHandler : IRequestHandler<GetOrderPickupDeliveriesQuery, List<OrderPickupDeliveryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetOrderPickupDeliveriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<OrderPickupDeliveryDto>> Handle(GetOrderPickupDeliveriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.PickupDeliveries
            .Where(p => p.OrderId == request.OrderId)
            .Include(p => p.DeliveryBoy)
            .OrderBy(p => p.IsPickup ? 0 : 1)
            .Select(p => new OrderPickupDeliveryDto(
                p.Id, p.IsPickup, p.Status, p.ScheduledAtUtc, p.CompletedAtUtc,
                p.DeliveryBoyEmployeeId, p.DeliveryBoy != null ? p.DeliveryBoy.FullName : null))
            .ToListAsync(cancellationToken);
    }
}
