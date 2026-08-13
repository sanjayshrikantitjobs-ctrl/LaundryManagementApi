using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.SubscriptionPlans.Commands.UpdateSubscriptionPlan;

public record UpdateSubscriptionPlanCommand(
    Guid PlanId, string Name, string? Description, BillingCycle BillingCycle, int GarmentsPerCycle,
    decimal Price, int DisplayOrder, bool IsActive, List<string> Features) : IRequest;

public class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
{
    public UpdateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.GarmentsPerCycle).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Features).NotEmpty().MaximumLength(200);
    }
}

public class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateSubscriptionPlanCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.SubscriptionPlans
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException($"SubscriptionPlan {request.PlanId} not found.");

        plan.Name = request.Name.Trim();
        plan.Description = request.Description;
        plan.BillingCycle = request.BillingCycle;
        plan.GarmentsPerCycle = request.GarmentsPerCycle;
        plan.Price = request.Price;
        plan.DisplayOrder = request.DisplayOrder;
        plan.IsActive = request.IsActive;

        // Add new features via the DbSet directly (not plan.Features.Add) — a client-generated
        // Guid Id on an object only reachable through an already-tracked parent's collection
        // navigation makes EF's change-detection infer "existing row" (UPDATE) instead of
        // "new row" (INSERT), since the key already looks non-default. Adding through the
        // DbSet forces EntityState.Added unambiguously, same pattern as AddCustomerAddressCommand.
        plan.Features.Clear();
        foreach (var (text, index) in request.Features.Select((text, index) => (text, index)))
            _db.SubscriptionPlanFeatures.Add(new SubscriptionPlanFeature { SubscriptionPlanId = plan.Id, Text = text, DisplayOrder = index });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
