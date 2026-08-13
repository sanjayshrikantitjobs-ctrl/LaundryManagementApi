using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Queries.GetCustomers;

public record CustomerListItemDto(
    Guid Id, string FullName, string PhoneNumber, string? Email, string? WhatsAppNumber,
    MembershipTier MembershipTier, string MembershipLabel, decimal WalletBalance, int LoyaltyPoints,
    CustomerStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastOrderAtUtc);

public record GetCustomersQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null,
    bool? HasOrders = null,
    CustomerStatus? Status = null) : IRequest<PaginatedList<CustomerListItemDto>>;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PaginatedList<CustomerListItemDto>>
{
    private static readonly Dictionary<string, System.Linq.Expressions.Expression<Func<Domain.Entities.Customer, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fullName"] = c => c.FullName,
        ["phoneNumber"] = c => c.PhoneNumber,
        ["membershipTier"] = c => c.MembershipTier,
        ["walletBalance"] = c => c.WalletBalance,
        ["createdAtUtc"] = c => c.CreatedAtUtc
    };

    private readonly IApplicationDbContext _db;

    public GetCustomersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<CustomerListItemDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(c =>
                c.FullName.Contains(request.Search) ||
                c.PhoneNumber.Contains(request.Search) ||
                (c.Email != null && c.Email.Contains(request.Search)));

        if (request.HasOrders.HasValue)
            query = request.HasOrders.Value ? query.Where(c => c.Orders.Any()) : query.Where(c => !c.Orders.Any());

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySort(request.SortBy, request.SortDirection, SortableColumns, c => c.FullName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerListItemDto(
                c.Id, c.FullName, c.PhoneNumber, c.Email, c.WhatsAppNumber, c.MembershipTier,
                _db.CustomerSubscriptions
                    .Where(s => s.CustomerId == c.Id && s.Status == SubscriptionStatus.Active)
                    .OrderByDescending(s => s.CreatedAtUtc)
                    .Select(s => s.SubscriptionPlan!.Name)
                    .FirstOrDefault() ?? "None",
                c.WalletBalance, c.LoyaltyPoints,
                c.Status, c.CreatedAtUtc, c.Orders.OrderByDescending(o => o.CreatedAtUtc).Select(o => (DateTimeOffset?)o.CreatedAtUtc).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PaginatedList<CustomerListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
