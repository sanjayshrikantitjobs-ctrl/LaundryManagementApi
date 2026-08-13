using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.ContactMessages.Queries.GetMyContactMessages;

public record MyContactMessageDto(
    Guid Id, ContactMessageType Type, string Message, string? ImageUrl,
    ContactMessageStatus Status, string? Response, DateTimeOffset CreatedAtUtc);

public record GetMyContactMessagesQuery : IRequest<List<MyContactMessageDto>>;

public class GetMyContactMessagesQueryHandler : IRequestHandler<GetMyContactMessagesQuery, List<MyContactMessageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyContactMessagesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<MyContactMessageDto>> Handle(GetMyContactMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        return await _db.ContactMessages
            .Where(m => m.Customer!.IdentityUserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => new MyContactMessageDto(m.Id, m.Type, m.Message, m.ImageUrl, m.Status, m.Response, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
