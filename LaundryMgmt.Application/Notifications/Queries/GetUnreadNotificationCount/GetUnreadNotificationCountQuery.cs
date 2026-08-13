using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Notifications.Queries.GetUnreadNotificationCount;

public record GetUnreadNotificationCountQuery : IRequest<int>;

public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadNotificationCountQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var isManagement = _currentUser.Role is "Admin" or "StoreManager" or "Staff";

        var query = isManagement
            ? _db.Notifications.Where(n => n.RecipientRole == "Admin")
            : _db.Notifications.Where(n => n.RecipientUserId == _currentUser.UserId);

        return await query.CountAsync(n => !n.IsRead, cancellationToken);
    }
}
