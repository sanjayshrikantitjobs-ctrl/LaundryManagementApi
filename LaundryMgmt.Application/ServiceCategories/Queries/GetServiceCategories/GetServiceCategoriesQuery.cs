using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.ServiceCategories.Queries.GetServiceCategories;

public record ServiceCategoryDto(Guid Id, string Name, string? Description, string? IconOrImageUrl, int DisplayOrder, bool IsActive);

/// <summary>Small, admin-managed list (~12 rows) — no pagination needed.</summary>
public record GetServiceCategoriesQuery : IRequest<List<ServiceCategoryDto>>;

public class GetServiceCategoriesQueryHandler : IRequestHandler<GetServiceCategoriesQuery, List<ServiceCategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetServiceCategoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ServiceCategoryDto>> Handle(GetServiceCategoriesQuery request, CancellationToken cancellationToken) =>
        await _db.ServiceCategories
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new ServiceCategoryDto(c.Id, c.Name, c.Description, c.IconOrImageUrl, c.DisplayOrder, c.IsActive))
            .ToListAsync(cancellationToken);
}
