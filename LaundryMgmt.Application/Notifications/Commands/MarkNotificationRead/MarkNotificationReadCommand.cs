using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var isManagement = _currentUser.Role is "Admin" or "StoreManager" or "Staff";

        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Notification {request.NotificationId} not found.");

        var isMine = isManagement ? notification.RecipientRole == "Admin" : notification.RecipientUserId == _currentUser.UserId;
        if (!isMine)
            throw new UnauthorizedAccessException("This notification isn't addressed to you.");

        notification.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
