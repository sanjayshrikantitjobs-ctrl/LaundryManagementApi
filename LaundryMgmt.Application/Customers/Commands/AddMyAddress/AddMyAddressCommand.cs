using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.AddMyAddress;

/// <summary>Lets the logged-in customer add an address to their own profile — resolves
/// the target Customer from the caller's identity rather than trusting a client-supplied id.</summary>
public record AddMyAddressCommand(
    string Label, string Line1, string? Line2, string City, string State, string PostalCode, bool IsDefault) : IRequest<Guid>;

public class AddMyAddressCommandValidator : AbstractValidator<AddMyAddressCommand>
{
    public AddMyAddressCommandValidator()
    {
        RuleFor(x => x.Line1).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.PostalCode).NotEmpty();
    }
}

public class AddMyAddressCommandHandler : IRequestHandler<AddMyAddressCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddMyAddressCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddMyAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        var customerId = await _db.Customers
            .Where(c => c.IdentityUserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No customer profile is linked to this login.");

        if (request.IsDefault)
        {
            var existingAddresses = await _db.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var existing in existingAddresses)
                existing.IsDefault = false;
        }

        var address = new CustomerAddress
        {
            CustomerId = customerId,
            Label = request.Label,
            Line1 = request.Line1,
            Line2 = request.Line2,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            IsDefault = request.IsDefault
        };

        _db.CustomerAddresses.Add(address);
        await _db.SaveChangesAsync(cancellationToken);

        return address.Id;
    }
}
