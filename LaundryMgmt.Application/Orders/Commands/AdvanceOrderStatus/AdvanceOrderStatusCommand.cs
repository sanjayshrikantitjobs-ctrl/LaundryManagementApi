using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Orders.Commands.AdvanceOrderStatus;

public record AdvanceOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : IRequest;

public class AdvanceOrderStatusCommandHandler : IRequestHandler<AdvanceOrderStatusCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public AdvanceOrderStatusCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task Handle(AdvanceOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found.");

        var history = order.AdvanceTo(request.NewStatus, _currentUser.UserName);
        _db.OrderStatusHistories.Add(history);

        await _db.SaveChangesAsync(cancellationToken);

        // Fire-and-forget style; in Infrastructure this gets queued via Hangfire
        // so a slow SMS/WhatsApp provider never blocks the API response.
        await _notifications.SendOrderStatusNotificationAsync(order.Id, cancellationToken);
    }
}
