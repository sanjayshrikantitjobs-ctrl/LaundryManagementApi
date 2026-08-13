using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.CustomerSubscriptions.Commands.AssignCustomerSubscription;

public record AssignCustomerSubscriptionCommand(
    Guid CustomerId, Guid SubscriptionPlanId, decimal RecurringValue,
    DateOnly StartDate, string? Notes) : IRequest<Guid>;

public class AssignCustomerSubscriptionCommandValidator : AbstractValidator<AssignCustomerSubscriptionCommand>
{
    public AssignCustomerSubscriptionCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SubscriptionPlanId).NotEmpty();
        RuleFor(x => x.RecurringValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class AssignCustomerSubscriptionCommandHandler : IRequestHandler<AssignCustomerSubscriptionCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public AssignCustomerSubscriptionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(AssignCustomerSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId, cancellationToken)
            ?? throw new KeyNotFoundException($"SubscriptionPlan {request.SubscriptionPlanId} not found.");

        var subscription = new CustomerSubscription
        {
            CustomerId = request.CustomerId,
            SubscriptionPlanId = request.SubscriptionPlanId,
            RecurringValue = request.RecurringValue,
            StartDate = request.StartDate,
            EndDate = request.StartDate.AddDays(BillingCycleDays(plan.BillingCycle)),
            Notes = request.Notes
        };

        _db.CustomerSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }

    /// <summary>Membership validity length per billing cycle, per the product spec
    /// (e.g. Monthly -&gt; 30 days).</summary>
    internal static int BillingCycleDays(BillingCycle cycle) => cycle switch
    {
        BillingCycle.Monthly => 30,
        BillingCycle.Quarterly => 90,
        BillingCycle.HalfYearly => 182,
        BillingCycle.Yearly => 365,
        _ => 30
    };
}
