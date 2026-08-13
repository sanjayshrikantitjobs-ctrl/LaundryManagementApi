using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Garments.Queries.GetGarmentById;

public record GarmentServicePriceDto(Guid ServiceId, string ServiceName, PricingType PricingType, decimal Price);

public record GarmentDetailDto(
    Guid Id, string Name, Guid CategoryId, string CategoryName, string? SpecialInstructions, string? ImageUrl,
    List<GarmentServicePriceDto> ServicePrices);

public record GetGarmentByIdQuery(Guid GarmentId) : IRequest<GarmentDetailDto>;

public class GetGarmentByIdQueryHandler : IRequestHandler<GetGarmentByIdQuery, GarmentDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetGarmentByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<GarmentDetailDto> Handle(GetGarmentByIdQuery request, CancellationToken cancellationToken)
    {
        var garment = await _db.Garments
            .Include(g => g.Category)
            .Include(g => g.ServicePrices).ThenInclude(p => p.Service)
            .FirstOrDefaultAsync(g => g.Id == request.GarmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Garment {request.GarmentId} not found.");

        return new GarmentDetailDto(
            garment.Id, garment.Name, garment.CategoryId, garment.Category!.Name, garment.SpecialInstructions, garment.ImageUrl,
            garment.ServicePrices.Select(p => new GarmentServicePriceDto(
                p.ServiceId, p.Service!.Name, p.PricingType, p.Price)).ToList());
    }
}
