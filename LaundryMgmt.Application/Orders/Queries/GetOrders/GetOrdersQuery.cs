using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Orders.Queries.GetOrders;

public record OrderListItemDto(
    Guid Id, string OrderNumber, string CustomerName, OrderStatus Status,
    decimal TotalAmount, PaymentStatus PaymentStatus, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PromisedByUtc);

public record GetOrdersQuery(
    OrderStatus? Status = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null,
    List<OrderStatus>? Statuses = null) : IRequest<PaginatedList<OrderListItemDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PaginatedList<OrderListItemDto>>
{
    internal static readonly Dictionary<string, System.Linq.Expressions.Expression<Func<Domain.Entities.Order, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["orderNumber"] = o => o.OrderNumber,
        ["customerName"] = o => o.Customer!.FullName,
        ["status"] = o => o.Status,
        ["totalAmount"] = o => o.TotalAmount,
        ["paymentStatus"] = o => o.PaymentStatus,
        ["createdAtUtc"] = o => o.CreatedAtUtc,
        ["promisedByUtc"] = o => o.PromisedByUtc!
    };

    private readonly IApplicationDbContext _db;

    public GetOrdersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Orders.Include(o => o.Customer).AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        if (request.Statuses is { Count: > 0 })
            query = query.Where(o => request.Statuses.Contains(o.Status));

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(o =>
                o.OrderNumber.Contains(request.Search) ||
                (o.Customer != null && o.Customer.FullName.Contains(request.Search)));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySort(request.SortBy, request.SortDirection, SortableColumns, o => o.CreatedAtUtc, defaultDescending: true)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderListItemDto(
                o.Id, o.OrderNumber, o.Customer!.FullName, o.Status,
                o.TotalAmount, o.PaymentStatus, o.CreatedAtUtc, o.PromisedByUtc))
            .ToListAsync(cancellationToken);

        return new PaginatedList<OrderListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
