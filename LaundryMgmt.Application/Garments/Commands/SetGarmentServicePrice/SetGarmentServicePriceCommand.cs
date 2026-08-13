using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Garments.Commands.SetGarmentServicePrice;

/// <summary>Upserts the price (and optional per-item overrides) for a Garment + Service
/// combination (e.g. Cotton Shirt + Steam Iron = 30). The four nullable overrides fall
/// back to the Service's own defaults when null — see GarmentServicePrice.</summary>
public record SetGarmentServicePriceCommand(
    Guid GarmentId, Guid ServiceId, PricingType PricingType, decimal Price,
    decimal? ExpressPrice = null, decimal? GstPercentage = null,
    int? EstimatedTimeHours = null, int? ExpressEtaHours = null, bool IsActive = true) : IRequest<Guid>;

public class SetGarmentServicePriceCommandValidator : AbstractValidator<SetGarmentServicePriceCommand>
{
    public SetGarmentServicePriceCommandValidator()
    {
        RuleFor(x => x.GarmentId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpressPrice).GreaterThanOrEqualTo(0).When(x => x.ExpressPrice.HasValue);
        RuleFor(x => x.GstPercentage).InclusiveBetween(0, 100).When(x => x.GstPercentage.HasValue);
        RuleFor(x => x.EstimatedTimeHours).GreaterThan(0).When(x => x.EstimatedTimeHours.HasValue);
        RuleFor(x => x.ExpressEtaHours).GreaterThan(0).When(x => x.ExpressEtaHours.HasValue);
    }
}

public class SetGarmentServicePriceCommandHandler : IRequestHandler<SetGarmentServicePriceCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SetGarmentServicePriceCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SetGarmentServicePriceCommand request, CancellationToken cancellationToken)
    {
        var garmentExists = await _db.Garments.AnyAsync(g => g.Id == request.GarmentId, cancellationToken);
        if (!garmentExists)
            throw new KeyNotFoundException($"Garment {request.GarmentId} not found.");

        var serviceExists = await _db.Services.AnyAsync(s => s.Id == request.ServiceId, cancellationToken);
        if (!serviceExists)
            throw new KeyNotFoundException($"Service {request.ServiceId} not found.");

        var priceEntry = await _db.GarmentServicePrices.FirstOrDefaultAsync(
            p => p.GarmentId == request.GarmentId && p.ServiceId == request.ServiceId, cancellationToken);

        if (priceEntry is null)
        {
            priceEntry = new GarmentServicePrice
            {
                GarmentId = request.GarmentId,
                ServiceId = request.ServiceId
            };
            _db.GarmentServicePrices.Add(priceEntry);
        }

        priceEntry.PricingType = request.PricingType;
        priceEntry.Price = request.Price;
        priceEntry.ExpressPrice = request.ExpressPrice;
        priceEntry.GstPercentage = request.GstPercentage;
        priceEntry.EstimatedTimeHours = request.EstimatedTimeHours;
        priceEntry.ExpressEtaHours = request.ExpressEtaHours;
        priceEntry.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return priceEntry.Id;
    }
}
