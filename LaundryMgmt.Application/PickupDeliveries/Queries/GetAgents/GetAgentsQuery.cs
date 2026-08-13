using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.PickupDeliveries.Queries.GetAgents;

public record AgentDto(Guid EmployeeId, string FullName);

/// <summary>Active Pickup/Delivery Agent employees, for the "assign agent" dropdown
/// on the order-detail screen. Management-only.</summary>
public record GetAgentsQuery(UserRole Role) : IRequest<List<AgentDto>>;

public class GetAgentsQueryHandler : IRequestHandler<GetAgentsQuery, List<AgentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAgentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<AgentDto>> Handle(GetAgentsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Employees
            .Where(e => e.Role == request.Role && e.IsActive)
            .OrderBy(e => e.FullName)
            .Select(e => new AgentDto(e.Id, e.FullName))
            .ToListAsync(cancellationToken);
    }
}
