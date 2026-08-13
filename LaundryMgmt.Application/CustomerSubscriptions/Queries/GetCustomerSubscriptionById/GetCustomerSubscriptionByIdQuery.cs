using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.CustomerSubscriptions.Queries.GetCustomerSubscriptionById;

public record CustomerSubscriptionDetailDto(
    Guid Id, Guid CustomerId, string CustomerName, Guid SubscriptionPlanId, string PlanName,
    decimal RecurringValue, DateOnly StartDate, DateOnly EndDate, DateOnly? NextBillingDate, SubscriptionStatus Status, string? Notes);

public record GetCustomerSubscriptionByIdQuery(Guid SubscriptionId) : IRequest<CustomerSubscriptionDetailDto>;

public class GetCustomerSubscriptionByIdQueryHandler
    : IRequestHandler<GetCustomerSubscriptionByIdQuery, CustomerSubscriptionDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetCustomerSubscriptionByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerSubscriptionDetailDto> Handle(GetCustomerSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var subscription = await _db.CustomerSubscriptions
            .Include(s => s.Customer)
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"CustomerSubscription {request.SubscriptionId} not found.");

        return new CustomerSubscriptionDetailDto(
            subscription.Id, subscription.CustomerId, subscription.Customer!.FullName,
            subscription.SubscriptionPlanId, subscription.SubscriptionPlan!.Name,
            subscription.RecurringValue, subscription.StartDate, subscription.EndDate, subscription.NextBillingDate,
            subscription.Status, subscription.Notes);
    }
}
