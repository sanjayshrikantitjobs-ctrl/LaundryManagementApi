using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Orders.Queries.GetOrderById;

public record OrderItemAddOnDto(Guid Id, string Name, decimal Price);

public record OrderItemDetailDto(
    Guid Id, string GarmentName, Guid ServiceId, string ServiceName, int Quantity, decimal? WeightKg,
    decimal UnitPrice, decimal LineTotal, string? SpecialInstructions, List<OrderItemAddOnDto> AddOns);

public record OrderDetailDto(
    Guid Id, string OrderNumber, OrderStatus Status, OrderChannel Channel, bool IsExpress, bool IsSameDay,
    decimal SubTotal, decimal DiscountAmount, string? PromoCode, decimal GstAmount, decimal DeliveryCharge, decimal TotalAmount,
    PaymentStatus PaymentStatus, decimal AmountPaid,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? PromisedByUtc, DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? PickupScheduledAtUtc,
    List<OrderItemDetailDto> Items);

/// <summary>Full detail for a single order — the drill-down behind "My Orders"/order
/// history. A Customer caller may only fetch their own order; management roles may
/// fetch any order.</summary>
public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailDto>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrderByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrderDetailDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Garment)
            .Include(o => o.Items).ThenInclude(i => i.Service)
            .Include(o => o.Items).ThenInclude(i => i.AddOns)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found.");

        if (string.Equals(_currentUser.Role, "Customer", StringComparison.OrdinalIgnoreCase) &&
            order.Customer?.IdentityUserId != _currentUser.UserId)
        {
            throw new UnauthorizedAccessException("You can only view your own orders.");
        }

        var pickupScheduledAtUtc = await _db.PickupDeliveries
            .Where(p => p.OrderId == order.Id && p.IsPickup)
            .Select(p => (DateTimeOffset?)p.ScheduledAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new OrderDetailDto(
            order.Id, order.OrderNumber, order.Status, order.Channel, order.IsExpress, order.IsSameDay,
            order.SubTotal, order.DiscountAmount, order.PromoCode, order.GstAmount, order.DeliveryCharge, order.TotalAmount,
            order.PaymentStatus, order.AmountPaid,
            order.CreatedAtUtc, order.PromisedByUtc, order.DeliveredAtUtc, pickupScheduledAtUtc,
            order.Items.Select(i => new OrderItemDetailDto(
                i.Id, i.Garment!.Name, i.ServiceId, i.Service!.Name, i.Quantity, i.WeightKg,
                i.UnitPrice, i.LineTotal, i.SpecialInstructions,
                i.AddOns.Select(a => new OrderItemAddOnDto(a.Id, a.Name, a.Price)).ToList())).ToList());
    }
}
