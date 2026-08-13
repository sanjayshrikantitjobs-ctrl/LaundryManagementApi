using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.AddOns.Queries.GetAddOns;

public record AddOnDto(Guid Id, string Name, string? Description, decimal Price, bool IsActive);

public record GetAddOnsQuery(bool? IsActive = null) : IRequest<List<AddOnDto>>;

public class GetAddOnsQueryHandler : IRequestHandler<GetAddOnsQuery, List<AddOnDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAddOnsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<AddOnDto>> Handle(GetAddOnsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AddOns.AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(a => a.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(a => a.Name)
            .Select(a => new AddOnDto(a.Id, a.Name, a.Description, a.Price, a.IsActive))
            .ToListAsync(cancellationToken);
    }
}
