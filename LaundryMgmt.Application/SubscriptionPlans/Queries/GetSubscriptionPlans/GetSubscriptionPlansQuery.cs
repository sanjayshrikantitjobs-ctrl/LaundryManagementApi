using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.SubscriptionPlans.Queries.GetSubscriptionPlans;

public record SubscriptionPlanFeatureDto(Guid Id, string Text, int DisplayOrder);

public record SubscriptionPlanDto(
    Guid Id, string Name, string? Description, BillingCycle BillingCycle, int GarmentsPerCycle,
    decimal Price, int DisplayOrder, bool IsActive, List<SubscriptionPlanFeatureDto> Features);

/// <summary>Small, admin-managed list — no pagination needed, mirrors GetServiceCategoriesQuery.</summary>
public record GetSubscriptionPlansQuery : IRequest<List<SubscriptionPlanDto>>;

public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, List<SubscriptionPlanDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSubscriptionPlansQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _db.SubscriptionPlans
            .Include(p => p.Features)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return plans.Select(p => new SubscriptionPlanDto(
            p.Id, p.Name, p.Description, p.BillingCycle, p.GarmentsPerCycle, p.Price, p.DisplayOrder, p.IsActive,
            p.Features.OrderBy(f => f.DisplayOrder)
                .Select(f => new SubscriptionPlanFeatureDto(f.Id, f.Text, f.DisplayOrder)).ToList())).ToList();
    }
}
