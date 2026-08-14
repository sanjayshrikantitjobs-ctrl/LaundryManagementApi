using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Orders.Commands.CreateOrder;

public record CreateOrderItemDto(
    Guid GarmentId, Guid ServiceId, int Quantity, decimal? WeightKg, string? SpecialInstructions, List<Guid>? AddOnIds = null);

public record CreateOrderCommand(
    Guid CustomerId,
    OrderChannel Channel,
    bool IsExpress,
    List<CreateOrderItemDto> Items,
    DateTimeOffset? PreferredPickupAtUtc = null,
    Guid? PickupAddressId = null,
    string? PromoCode = null,
    bool IsSameDay = false,
    DateTimeOffset? PreferredDeliveryAtUtc = null) : IRequest<Guid>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("An order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.GarmentId).NotEmpty();
            item.RuleFor(i => i.ServiceId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;

    public CreateOrderCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider dateTime)
    {
        _db = db;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // A Customer-role caller may only ever place an order under their own linked
        // Customer record — otherwise they could pass any CustomerId in the request body
        // and place orders (and see the resulting invoice) under someone else's identity.
        if (string.Equals(_currentUser.Role, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            var ownCustomerId = await _db.Customers
                .Where(c => c.IdentityUserId == _currentUser.UserId)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (ownCustomerId != request.CustomerId)
                throw new UnauthorizedAccessException("You can only place orders for your own account.");
        }

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            CustomerId = request.CustomerId,
            Channel = request.Channel,
            IsExpress = request.IsExpress,
            IsSameDay = request.IsSameDay
        };

        var maxEtaHours = 0;

        foreach (var itemDto in request.Items)
        {
            var priceEntry = await _db.GarmentServicePrices
                .Include(p => p.Service)
                .FirstOrDefaultAsync(p => p.GarmentId == itemDto.GarmentId && p.ServiceId == itemDto.ServiceId, cancellationToken)
                ?? throw new DomainException(
                    $"No price configured for garment {itemDto.GarmentId} + service {itemDto.ServiceId}.");

            if (!priceEntry.IsActive)
                throw new DomainException(
                    $"Garment {itemDto.GarmentId} is not currently available under service {itemDto.ServiceId}.");

            var service = priceEntry.Service!;
            var isWeightBased = priceEntry.PricingType == PricingType.WeightBased;

            // Per-item overrides win when set; otherwise fall back to the Service's own
            // defaults. IsExpress (not Channel, which is purely about fulfillment method)
            // is the single driver of express pricing/ETA.
            decimal unitPrice;
            if (request.IsExpress && priceEntry.ExpressPrice.HasValue)
            {
                unitPrice = isWeightBased && itemDto.WeightKg.HasValue
                    ? priceEntry.ExpressPrice.Value * itemDto.WeightKg.Value
                    : priceEntry.ExpressPrice.Value;
            }
            else
            {
                unitPrice = isWeightBased && itemDto.WeightKg.HasValue
                    ? priceEntry.Price * itemDto.WeightKg.Value
                    : priceEntry.Price;

                if (request.IsExpress)
                    unitPrice += service.ExpressSurcharge;
            }

            var lineTotal = isWeightBased ? unitPrice : unitPrice * itemDto.Quantity;

            var orderItem = new OrderItem
            {
                GarmentId = itemDto.GarmentId,
                ServiceId = itemDto.ServiceId,
                Quantity = itemDto.Quantity,
                WeightKg = itemDto.WeightKg,
                UnitPrice = unitPrice,
                SpecialInstructions = itemDto.SpecialInstructions
            };

            if (itemDto.AddOnIds is { Count: > 0 })
            {
                var addOns = await _db.AddOns
                    .Where(a => itemDto.AddOnIds.Contains(a.Id) && a.IsActive)
                    .ToListAsync(cancellationToken);

                if (addOns.Count != itemDto.AddOnIds.Distinct().Count())
                    throw new DomainException("One or more selected add-ons are unavailable.");

                foreach (var addOn in addOns)
                {
                    orderItem.AddOns.Add(new OrderItemAddOn { AddOnId = addOn.Id, Name = addOn.Name, Price = addOn.Price });
                    lineTotal += addOn.Price; // once per line, not per unit
                }
            }

            orderItem.LineTotal = lineTotal;
            order.Items.Add(orderItem);

            var gstPercentage = priceEntry.GstPercentage ?? service.GstPercentage;
            order.GstAmount += Math.Round(lineTotal * gstPercentage / 100m, 2);

            var etaHours = request.IsExpress
                ? priceEntry.ExpressEtaHours ?? service.ExpressEtaHours
                : priceEntry.EstimatedTimeHours ?? service.EstimatedTimeHours;
            maxEtaHours = Math.Max(maxEtaHours, etaHours);
        }

        order.SubTotal = order.Items.Sum(i => i.LineTotal);

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var now = _dateTime.UtcNow;
            var normalizedCode = request.PromoCode.Trim().ToUpperInvariant();

            // ValidFrom/ValidTo are optional (an unset bound means "no restriction on that
            // side"), matching how promotions are created/displayed elsewhere (see
            // GetActivePromotionsQuery) — a promo with no expiry must stay redeemable.
            var promotion = await _db.Promotions.FirstOrDefaultAsync(p =>
                p.Code != null && p.Code == normalizedCode &&
                p.IsActive &&
                (!p.ValidFrom.HasValue || p.ValidFrom <= now) &&
                (!p.ValidTo.HasValue || p.ValidTo >= now),
                cancellationToken)
                ?? throw new DomainException($"Promo code '{request.PromoCode}' is invalid or has expired.");

            var discount = promotion.DiscountPercent.HasValue
                ? order.SubTotal * promotion.DiscountPercent.Value / 100m
                : promotion.DiscountAmount ?? 0m;

            order.DiscountAmount = Math.Round(Math.Min(discount, order.SubTotal), 2);
            order.PromoCode = normalizedCode;
        }
        else
        {
            order.DiscountAmount = 0m;
            order.PromoCode = null;
        }

        order.TotalAmount = order.SubTotal + order.GstAmount - order.DiscountAmount + order.DeliveryCharge;
        order.PromisedByUtc = DateTimeOffset.UtcNow.AddHours(maxEtaHours);

        _db.Orders.Add(order);

        var customerName = await _db.Customers
            .Where(c => c.Id == request.CustomerId)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        _db.Notifications.Add(new Notification
        {
            RecipientRole = "Admin",
            Type = NotificationType.NewOrder,
            Title = "New order placed",
            Message = $"{customerName ?? "A customer"} placed order {order.OrderNumber} (₹{order.TotalAmount}).",
            EntityId = order.Id.ToString()
        });

        if (request.PreferredPickupAtUtc.HasValue || request.PreferredDeliveryAtUtc.HasValue)
        {
            Guid? addressId = null;
            if (request.PickupAddressId.HasValue)
            {
                var addressBelongsToCustomer = await _db.CustomerAddresses.AnyAsync(
                    a => a.Id == request.PickupAddressId.Value && a.CustomerId == request.CustomerId, cancellationToken);
                addressId = addressBelongsToCustomer ? request.PickupAddressId : null;
            }

            if (request.PreferredPickupAtUtc.HasValue)
            {
                _db.PickupDeliveries.Add(new PickupDelivery
                {
                    Order = order,
                    IsPickup = true,
                    AddressId = addressId,
                    Status = DeliveryStatus.Scheduled,
                    ScheduledAtUtc = request.PreferredPickupAtUtc
                });
            }

            // The customer's requested delivery slot — a preference for staff to plan
            // around, same as PreferredPickupAtUtc; the delivery isn't actually
            // dispatched until an admin/delivery agent moves it through the pipeline.
            if (request.PreferredDeliveryAtUtc.HasValue)
            {
                _db.PickupDeliveries.Add(new PickupDelivery
                {
                    Order = order,
                    IsPickup = false,
                    AddressId = addressId,
                    Status = DeliveryStatus.Scheduled,
                    ScheduledAtUtc = request.PreferredDeliveryAtUtc
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(100000, 999999)}";
}
