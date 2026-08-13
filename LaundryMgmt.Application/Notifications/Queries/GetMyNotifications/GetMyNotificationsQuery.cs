using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Notifications.Queries.GetMyNotifications;

public record NotificationDto(
    Guid Id, NotificationType Type, string Title, string Message, string? EntityId, bool IsRead, DateTimeOffset CreatedAtUtc);

/// <summary>Management roles (Admin/StoreManager/Staff) share one inbox
/// (RecipientRole = "Admin"); everyone else sees only notifications addressed to
/// their own login (RecipientUserId).</summary>
public record GetMyNotificationsQuery(int Take = 30) : IRequest<List<NotificationDto>>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var isManagement = _currentUser.Role is "Admin" or "StoreManager" or "Staff";

        var query = isManagement
            ? _db.Notifications.Where(n => n.RecipientRole == "Admin")
            : _db.Notifications.Where(n => n.RecipientUserId == _currentUser.UserId);

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(request.Take)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Message, n.EntityId, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
