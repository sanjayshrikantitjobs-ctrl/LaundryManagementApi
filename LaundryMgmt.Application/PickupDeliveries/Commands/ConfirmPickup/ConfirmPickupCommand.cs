using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.PickupDeliveries.Commands.ConfirmPickup;

/// <summary>The Pickup Agent confirms garments have been received from the customer.
/// Requires at least one Pickup-type image already uploaded — enforced here, not just
/// in the UI — then advances the order to Received and closes out the pickup leg.</summary>
public record ConfirmPickupCommand(Guid PickupDeliveryId) : IRequest;

public class ConfirmPickupCommandHandler : IRequestHandler<ConfirmPickupCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ConfirmPickupCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ConfirmPickupCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Not signed in.");

        var pickup = await _db.PickupDeliveries
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == request.PickupDeliveryId && p.IsPickup, cancellationToken)
            ?? throw new KeyNotFoundException($"Pickup {request.PickupDeliveryId} not found.");

        var employeeId = await _db.Employees
            .Where(e => e.IdentityUserId == userId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!employeeId.HasValue || pickup.DeliveryBoyEmployeeId != employeeId.Value)
            throw new UnauthorizedAccessException("This pickup isn't assigned to you.");

        var hasPickupImage = await _db.OrderGarmentImages.AnyAsync(
            i => i.OrderId == pickup.OrderId && i.ImageType == GarmentImageType.Pickup, cancellationToken);
        if (!hasPickupImage)
            throw new DomainException("Upload at least one garment photo before confirming pickup.");

        // AdvanceTo appends to Order.StatusHistory via a navigation-collection Add,
        // which EF Core can misdetect as Modified instead of Added on an
        // already-tracked parent — explicitly Add() the returned row to sidestep it
        // (same fix already applied to AdvanceOrderStatusCommandHandler/UpdateOrderCommandHandler).
        var history = pickup.Order!.AdvanceTo(OrderStatus.Received, changedBy: _currentUser.UserName);
        _db.OrderStatusHistories.Add(history);

        pickup.Status = DeliveryStatus.PickedUp;
        pickup.CompletedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
