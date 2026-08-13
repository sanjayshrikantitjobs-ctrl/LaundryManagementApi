using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Queries.GetCustomerById;

public record CustomerAddressDto(
    Guid Id, string Label, string Line1, string? Line2, string City, string State, string PostalCode, bool IsDefault);

public record CustomerDetailDto(
    Guid Id, string FullName, string PhoneNumber, string? Email,
    decimal WalletBalance, decimal CreditLimit, int LoyaltyPoints,
    MembershipTier MembershipTier, string MembershipLabel, CustomerStatus Status, string? Notes, List<CustomerAddressDto> Addresses);

public record GetCustomerByIdQuery(Guid CustomerId) : IRequest<CustomerDetailDto>;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetCustomerByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerDetailDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found.");

        var membershipLabel = await _db.CustomerSubscriptions
            .Where(s => s.CustomerId == customer.Id && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => s.SubscriptionPlan!.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "None";

        return new CustomerDetailDto(
            customer.Id, customer.FullName, customer.PhoneNumber, customer.Email,
            customer.WalletBalance, customer.CreditLimit, customer.LoyaltyPoints,
            customer.MembershipTier, membershipLabel, customer.Status, customer.Notes,
            customer.Addresses.Select(a => new CustomerAddressDto(
                a.Id, a.Label, a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.IsDefault)).ToList());
    }
}
