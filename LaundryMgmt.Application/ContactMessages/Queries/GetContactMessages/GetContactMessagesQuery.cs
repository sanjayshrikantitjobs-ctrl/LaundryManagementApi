using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.ContactMessages.Queries.GetContactMessages;

public record ContactMessageListItemDto(
    Guid Id, Guid CustomerId, string CustomerName, ContactMessageType Type, string Message, string? ImageUrl,
    ContactMessageStatus Status, string? Response, DateTimeOffset CreatedAtUtc);

/// <summary>Admin/staff inbox for all customer-submitted feedback/complaints/queries.</summary>
public record GetContactMessagesQuery(
    ContactMessageStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null) : IRequest<PaginatedList<ContactMessageListItemDto>>;

public class GetContactMessagesQueryHandler : IRequestHandler<GetContactMessagesQuery, PaginatedList<ContactMessageListItemDto>>
{
    private static readonly Dictionary<string, System.Linq.Expressions.Expression<Func<Domain.Entities.ContactMessage, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["customerName"] = m => m.Customer!.FullName,
        ["type"] = m => m.Type,
        ["status"] = m => m.Status,
        ["createdAtUtc"] = m => m.CreatedAtUtc
    };

    private readonly IApplicationDbContext _db;

    public GetContactMessagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<ContactMessageListItemDto>> Handle(GetContactMessagesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ContactMessages.AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(m => m.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySort(request.SortBy, request.SortDirection, SortableColumns, m => m.CreatedAtUtc, defaultDescending: true)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new ContactMessageListItemDto(
                m.Id, m.CustomerId, m.Customer!.FullName, m.Type, m.Message, m.ImageUrl, m.Status, m.Response, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PaginatedList<ContactMessageListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
