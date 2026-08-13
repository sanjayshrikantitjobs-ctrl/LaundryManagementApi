using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;

namespace LaundryMgmt.Application.SubscriptionPlans.Commands.CreateSubscriptionPlan;

public record CreateSubscriptionPlanCommand(
    string Name, string? Description, BillingCycle BillingCycle, int GarmentsPerCycle,
    decimal Price, int DisplayOrder, List<string> Features) : IRequest<Guid>;

public class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
{
    public CreateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.GarmentsPerCycle).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Features).NotEmpty().MaximumLength(200);
    }
}

public class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateSubscriptionPlanCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new SubscriptionPlan
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            BillingCycle = request.BillingCycle,
            GarmentsPerCycle = request.GarmentsPerCycle,
            Price = request.Price,
            DisplayOrder = request.DisplayOrder
        };

        plan.Features = request.Features
            .Select((text, index) => new SubscriptionPlanFeature { Text = text, DisplayOrder = index })
            .ToList();

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
