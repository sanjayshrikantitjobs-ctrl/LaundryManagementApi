using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Customers.Queries.GetCustomers;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Queries.GetMyCustomerProfile;

/// <summary>Resolves the Customer catalog row linked to the logged-in identity
/// (Customer.IdentityUserId), so the customer portal can place orders/view its own
/// data without ever browsing or guessing another customer's id.</summary>
public record GetMyCustomerProfileQuery : IRequest<CustomerListItemDto>;

public class GetMyCustomerProfileQueryHandler : IRequestHandler<GetMyCustomerProfileQuery, CustomerListItemDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyCustomerProfileQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CustomerListItemDto> Handle(GetMyCustomerProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        return await _db.Customers
            .Where(c => c.IdentityUserId == userId)
            .Select(c => new CustomerListItemDto(
                c.Id, c.FullName, c.PhoneNumber, c.Email, c.WhatsAppNumber, c.MembershipTier,
                _db.CustomerSubscriptions
                    .Where(s => s.CustomerId == c.Id && s.Status == SubscriptionStatus.Active)
                    .OrderByDescending(s => s.CreatedAtUtc)
                    .Select(s => s.SubscriptionPlan!.Name)
                    .FirstOrDefault() ?? "None",
                c.WalletBalance, c.LoyaltyPoints,
                c.Status, c.CreatedAtUtc, c.Orders.OrderByDescending(o => o.CreatedAtUtc).Select(o => (DateTimeOffset?)o.CreatedAtUtc).FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No customer profile is linked to this login.");
    }
}
