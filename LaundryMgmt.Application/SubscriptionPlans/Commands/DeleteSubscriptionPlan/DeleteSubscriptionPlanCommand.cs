using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.SubscriptionPlans.Commands.DeleteSubscriptionPlan;

public record DeleteSubscriptionPlanCommand(Guid PlanId, string? Reason = null) : IRequest;

public class DeleteSubscriptionPlanCommandHandler : IRequestHandler<DeleteSubscriptionPlanCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteSubscriptionPlanCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException($"SubscriptionPlan {request.PlanId} not found.");

        // Restrict FK can't protect against this at the DB level — soft-delete turns
        // Remove() into an UPDATE (IsDeleted=true), not a real DELETE, so the FK
        // constraint never fires. Guard explicitly, same as DeleteServiceCategoryCommand.
        var hasSubscribers = await _db.CustomerSubscriptions.AnyAsync(s => s.SubscriptionPlanId == request.PlanId, cancellationToken);
        if (hasSubscribers)
            throw new DomainException("Move or cancel customers off this plan before deleting it.");

        plan.DeletedReason = request.Reason;
        _db.SubscriptionPlans.Remove(plan);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
