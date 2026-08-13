using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.CustomerSubscriptions.Queries.GetCustomerSubscriptions;

public record CustomerSubscriptionListItemDto(
    Guid Id, Guid CustomerId, string CustomerName, Guid SubscriptionPlanId, string PlanName,
    decimal RecurringValue, DateOnly StartDate, DateOnly EndDate, DateOnly? NextBillingDate, SubscriptionStatus Status, string? Notes);

public record GetCustomerSubscriptionsQuery(
    string? Search = null,
    Guid? CustomerId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<CustomerSubscriptionListItemDto>>;

public class GetCustomerSubscriptionsQueryHandler
    : IRequestHandler<GetCustomerSubscriptionsQuery, PaginatedList<CustomerSubscriptionListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCustomerSubscriptionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<CustomerSubscriptionListItemDto>> Handle(
        GetCustomerSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.CustomerSubscriptions
            .Include(s => s.Customer)
            .Include(s => s.SubscriptionPlan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(s => s.Customer!.FullName.Contains(request.Search));

        if (request.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == request.CustomerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new CustomerSubscriptionListItemDto(
                s.Id, s.CustomerId, s.Customer!.FullName, s.SubscriptionPlanId, s.SubscriptionPlan!.Name,
                s.RecurringValue, s.StartDate, s.EndDate, s.NextBillingDate, s.Status, s.Notes))
            .ToListAsync(cancellationToken);

        return new PaginatedList<CustomerSubscriptionListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
