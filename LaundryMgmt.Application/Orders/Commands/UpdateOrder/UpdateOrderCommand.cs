using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Orders.Commands.UpdateOrder;

/// <summary>Admin/staff order-management update: status (any target, not just the
/// next pipeline step — see Order.SetStatus), payment status/amount, and expected
/// delivery date. Any field left null is unchanged. Notifies the customer (if their
/// account has a linked login) about whichever fields actually changed.</summary>
public record UpdateOrderCommand(
    Guid OrderId,
    OrderStatus? Status,
    PaymentStatus? PaymentStatus,
    decimal? AmountPaid,
    DateTimeOffset? PromisedByUtc) : IRequest;

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0).When(x => x.AmountPaid.HasValue);
    }
}

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateOrderCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.StatusHistory)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found.");

        var changes = new List<string>();

        if (request.Status.HasValue && request.Status.Value != order.Status)
        {
            var history = order.SetStatus(request.Status.Value, _currentUser.UserName);
            if (history is not null)
                _db.OrderStatusHistories.Add(history);
            changes.Add($"status changed to {request.Status.Value}");
        }

        if (request.PaymentStatus.HasValue && request.PaymentStatus.Value != order.PaymentStatus)
        {
            order.PaymentStatus = request.PaymentStatus.Value;
            changes.Add($"payment status changed to {request.PaymentStatus.Value}");
        }

        if (request.AmountPaid.HasValue && request.AmountPaid.Value != order.AmountPaid)
        {
            order.AmountPaid = request.AmountPaid.Value;
            changes.Add("amount paid updated");
        }

        if (request.PromisedByUtc.HasValue && request.PromisedByUtc.Value != order.PromisedByUtc)
        {
            order.PromisedByUtc = request.PromisedByUtc.Value;
            changes.Add("expected delivery time updated");
        }

        if (changes.Count == 0)
            return;

        if (order.Customer?.IdentityUserId.HasValue == true)
        {
            _db.Notifications.Add(new Notification
            {
                RecipientUserId = order.Customer.IdentityUserId.Value,
                Type = NotificationType.OrderUpdated,
                Title = "Order updated",
                Message = $"Your order {order.OrderNumber} was updated: {string.Join(", ", changes)}.",
                EntityId = order.Id.ToString()
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
