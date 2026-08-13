using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Garments.Queries.GetGarments;

public record GarmentListItemDto(Guid Id, string Name, Guid CategoryId, string CategoryName, string? ImageUrl);

public record GetGarmentsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null) : IRequest<PaginatedList<GarmentListItemDto>>;

public class GetGarmentsQueryHandler : IRequestHandler<GetGarmentsQuery, PaginatedList<GarmentListItemDto>>
{
    private static readonly Dictionary<string, System.Linq.Expressions.Expression<Func<Domain.Entities.Garment, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = g => g.Name
    };

    private readonly IApplicationDbContext _db;

    public GetGarmentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<GarmentListItemDto>> Handle(GetGarmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Garments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(g => g.Name.Contains(request.Search));

        if (request.CategoryId.HasValue)
            query = query.Where(g => g.CategoryId == request.CategoryId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySort(request.SortBy, request.SortDirection, SortableColumns, g => g.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new GarmentListItemDto(g.Id, g.Name, g.CategoryId, g.Category!.Name, g.ImageUrl))
            .ToListAsync(cancellationToken);

        return new PaginatedList<GarmentListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
