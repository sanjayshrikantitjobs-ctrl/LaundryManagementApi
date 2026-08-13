using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Services.Queries.GetServices;

public record ServiceListItemDto(
    Guid Id, string Name, Guid CategoryId, string CategoryName, decimal BasePrice, int EstimatedTimeHours, decimal GstPercentage, int Priority, string? ImageUrl,
    decimal ExpressSurcharge, int ExpressEtaHours);

public record GetServicesQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null) : IRequest<PaginatedList<ServiceListItemDto>>;

public class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, PaginatedList<ServiceListItemDto>>
{
    private static readonly Dictionary<string, System.Linq.Expressions.Expression<Func<Domain.Entities.Service, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = s => s.Name,
        ["basePrice"] = s => s.BasePrice,
        ["estimatedTimeHours"] = s => s.EstimatedTimeHours,
        ["priority"] = s => s.Priority
    };

    private readonly IApplicationDbContext _db;

    public GetServicesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<ServiceListItemDto>> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Services.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(s => s.Name.Contains(request.Search));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySort(request.SortBy, request.SortDirection, SortableColumns, s => s.Priority)
            .ThenBy(s => s.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ServiceListItemDto(
                s.Id, s.Name, s.CategoryId, s.Category!.Name, s.BasePrice, s.EstimatedTimeHours, s.GstPercentage, s.Priority, s.ImageUrl,
                s.ExpressSurcharge, s.ExpressEtaHours))
            .ToListAsync(cancellationToken);

        return new PaginatedList<ServiceListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
