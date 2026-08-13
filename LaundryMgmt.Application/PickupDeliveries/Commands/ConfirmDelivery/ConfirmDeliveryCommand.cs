using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.PickupDeliveries.Commands.ConfirmDelivery;

/// <summary>The Delivery Agent confirms an order has been delivered. Requires at
/// least one Delivery-type (proof-of-delivery) image already uploaded, then advances
/// the order to Delivered and closes out the delivery leg.</summary>
public record ConfirmDeliveryCommand(Guid PickupDeliveryId) : IRequest;

public class ConfirmDeliveryCommandHandler : IRequestHandler<ConfirmDeliveryCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ConfirmDeliveryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ConfirmDeliveryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Not signed in.");

        var delivery = await _db.PickupDeliveries
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == request.PickupDeliveryId && !p.IsPickup, cancellationToken)
            ?? throw new KeyNotFoundException($"Delivery {request.PickupDeliveryId} not found.");

        var employeeId = await _db.Employees
            .Where(e => e.IdentityUserId == userId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!employeeId.HasValue || delivery.DeliveryBoyEmployeeId != employeeId.Value)
            throw new UnauthorizedAccessException("This delivery isn't assigned to you.");

        var hasDeliveryImage = await _db.OrderGarmentImages.AnyAsync(
            i => i.OrderId == delivery.OrderId && i.ImageType == GarmentImageType.Delivery, cancellationToken);
        if (!hasDeliveryImage)
            throw new DomainException("Upload at least one delivery-proof photo before confirming delivery.");

        // See ConfirmPickupCommandHandler for why the returned history row is added explicitly.
        var history = delivery.Order!.AdvanceTo(OrderStatus.Delivered, changedBy: _currentUser.UserName);
        _db.OrderStatusHistories.Add(history);

        delivery.Status = DeliveryStatus.Delivered;
        delivery.CompletedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
