using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.CustomerSubscriptions.Commands.UpdateCustomerSubscription;

public record UpdateCustomerSubscriptionCommand(
    Guid SubscriptionId, decimal RecurringValue, SubscriptionStatus Status,
    DateOnly? NextBillingDate, string? Notes) : IRequest;

public class UpdateCustomerSubscriptionCommandValidator : AbstractValidator<UpdateCustomerSubscriptionCommand>
{
    public UpdateCustomerSubscriptionCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.RecurringValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class UpdateCustomerSubscriptionCommandHandler : IRequestHandler<UpdateCustomerSubscriptionCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateCustomerSubscriptionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateCustomerSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _db.CustomerSubscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new KeyNotFoundException($"CustomerSubscription {request.SubscriptionId} not found.");

        subscription.RecurringValue = request.RecurringValue;
        subscription.Status = request.Status;
        subscription.NextBillingDate = request.NextBillingDate;
        subscription.Notes = request.Notes;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
