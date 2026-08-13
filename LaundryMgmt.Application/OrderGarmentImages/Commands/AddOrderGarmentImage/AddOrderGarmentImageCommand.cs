using FluentValidation;
using LaundryMgmt.Application.Common.Constants;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.OrderGarmentImages.Commands.AddOrderGarmentImage;

public record AddOrderGarmentImageCommand(
    Guid OrderId, GarmentImageType ImageType, string ImageUrl,
    Guid? ServiceId, Guid? OrderItemId, string? Notes) : IRequest<Guid>;

public class AddOrderGarmentImageCommandValidator : AbstractValidator<AddOrderGarmentImageCommand>
{
    public AddOrderGarmentImageCommandValidator()
    {
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class AddOrderGarmentImageCommandHandler : IRequestHandler<AddOrderGarmentImageCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddOrderGarmentImageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddOrderGarmentImageCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found.");

        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Not signed in.");
        var role = _currentUser.Role;

        // Pickup/Delivery agents may only attach the image type matching their role,
        // and only to an order that's actually assigned to them — never an arbitrary
        // order id supplied by the client.
        if (string.Equals(role, "PickupAgent", StringComparison.OrdinalIgnoreCase))
        {
            if (request.ImageType != GarmentImageType.Pickup)
                throw new UnauthorizedAccessException("Pickup Agents may only upload pickup images.");
            await EnsureAssignedAsync(userId, request.OrderId, isPickup: true, cancellationToken);
        }
        else if (string.Equals(role, "DeliveryAgent", StringComparison.OrdinalIgnoreCase))
        {
            if (request.ImageType != GarmentImageType.Delivery)
                throw new UnauthorizedAccessException("Delivery Agents may only upload delivery images.");
            await EnsureAssignedAsync(userId, request.OrderId, isPickup: false, cancellationToken);
        }
        // Admin/StoreManager/Staff/DepartmentHead: no additional ownership check —
        // management has full oversight (enforced at the controller via AppRoles.OperationalRoles).

        var existingCount = await _db.OrderGarmentImages
            .CountAsync(i => i.OrderId == request.OrderId && i.ImageType == request.ImageType, cancellationToken);
        if (existingCount >= AppConstants.MaxImagesPerOrderCategory)
            throw new DomainException(
                $"This order already has the maximum of {AppConstants.MaxImagesPerOrderCategory} {request.ImageType} images.");

        var image = new OrderGarmentImage
        {
            OrderId = request.OrderId,
            ServiceId = request.ServiceId,
            OrderItemId = request.OrderItemId,
            ImageType = request.ImageType,
            ImageUrl = request.ImageUrl,
            UploadedByUserId = userId,
            UploadedByName = _currentUser.UserName ?? "Unknown",
            UploadedAtUtc = DateTimeOffset.UtcNow,
            Notes = request.Notes
        };

        _db.OrderGarmentImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);

        return image.Id;
    }

    private async Task EnsureAssignedAsync(Guid identityUserId, Guid orderId, bool isPickup, CancellationToken cancellationToken)
    {
        var employeeId = await _db.Employees
            .Where(e => e.IdentityUserId == identityUserId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var isAssigned = employeeId.HasValue && await _db.PickupDeliveries.AnyAsync(
            p => p.OrderId == orderId && p.IsPickup == isPickup && p.DeliveryBoyEmployeeId == employeeId.Value,
            cancellationToken);

        if (!isAssigned)
            throw new UnauthorizedAccessException("This order isn't assigned to you.");
    }
}
