using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Garments.Queries.GetPricingMatrix;

public record PricingMatrixServiceDto(Guid Id, string Name, Guid CategoryId, string CategoryName);

public record PricingMatrixCellDto(
    Guid ServiceId, PricingType? PricingType, decimal? Price,
    decimal? ExpressPrice, decimal? GstPercentage, int? EstimatedTimeHours, int? ExpressEtaHours, bool IsActive);

public record PricingMatrixGarmentRowDto(Guid GarmentId, string GarmentName, Guid CategoryId, string CategoryName, List<PricingMatrixCellDto> Prices);

public record PricingMatrixDto(List<PricingMatrixServiceDto> Services, List<PricingMatrixGarmentRowDto> Garments);

/// <summary>Full Garment x Service price grid, used to render/edit the pricing matrix in one screen.</summary>
public record GetPricingMatrixQuery : IRequest<PricingMatrixDto>;

public class GetPricingMatrixQueryHandler : IRequestHandler<GetPricingMatrixQuery, PricingMatrixDto>
{
    private readonly IApplicationDbContext _db;

    public GetPricingMatrixQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PricingMatrixDto> Handle(GetPricingMatrixQuery request, CancellationToken cancellationToken)
    {
        var services = await _db.Services
            .OrderBy(s => s.Category!.DisplayOrder).ThenBy(s => s.Priority).ThenBy(s => s.Name)
            .Select(s => new PricingMatrixServiceDto(s.Id, s.Name, s.CategoryId, s.Category!.Name))
            .ToListAsync(cancellationToken);

        var garments = await _db.Garments
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.CategoryId,
                CategoryName = g.Category!.Name,
                Prices = g.ServicePrices.Select(p => new
                {
                    p.ServiceId, p.PricingType, p.Price,
                    p.ExpressPrice, p.GstPercentage, p.EstimatedTimeHours, p.ExpressEtaHours, p.IsActive
                })
            })
            .ToListAsync(cancellationToken);

        var rows = garments.Select(g =>
        {
            var pricesByService = g.Prices.ToDictionary(p => p.ServiceId);
            var cells = services.Select(s => pricesByService.TryGetValue(s.Id, out var p)
                ? new PricingMatrixCellDto(s.Id, p.PricingType, p.Price, p.ExpressPrice, p.GstPercentage, p.EstimatedTimeHours, p.ExpressEtaHours, p.IsActive)
                : new PricingMatrixCellDto(s.Id, null, null, null, null, null, null, true)).ToList();

            return new PricingMatrixGarmentRowDto(g.Id, g.Name, g.CategoryId, g.CategoryName, cells);
        }).ToList();

        return new PricingMatrixDto(services, rows);
    }
}
