using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.UpdateCustomer;

// Membership is no longer a manually-set field here — it's derived from the customer's
// active CustomerSubscription (see GetCustomers/GetCustomerById's MembershipLabel) and
// managed via the Subscriptions screens instead.
public record UpdateCustomerCommand(
    Guid CustomerId,
    string FullName,
    string PhoneNumber,
    string? Email,
    decimal CreditLimit,
    string? Notes) : IRequest;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateCustomerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found.");

        var normalizedPhone = request.PhoneNumber.Trim();
        var phoneInUseByAnother = await _db.Customers
            .AnyAsync(c => c.Id != request.CustomerId && c.PhoneNumber == normalizedPhone, cancellationToken);
        if (phoneInUseByAnother)
            throw new InvalidOperationException($"A customer with phone number {normalizedPhone} already exists.");

        customer.FullName = request.FullName.Trim();
        customer.PhoneNumber = normalizedPhone;
        customer.Email = request.Email?.Trim();
        customer.CreditLimit = request.CreditLimit;
        customer.Notes = request.Notes;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
