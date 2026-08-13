using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.AddCustomerAddress;

public record AddCustomerAddressCommand(
    Guid CustomerId,
    string Label,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    bool IsDefault) : IRequest<Guid>;

public class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Line1).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.PostalCode).NotEmpty();
    }
}

public class AddCustomerAddressCommandHandler : IRequestHandler<AddCustomerAddressCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public AddCustomerAddressCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(AddCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
            throw new KeyNotFoundException($"Customer {request.CustomerId} not found.");

        if (request.IsDefault)
        {
            var existingAddresses = await _db.CustomerAddresses
                .Where(a => a.CustomerId == request.CustomerId && a.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var existing in existingAddresses)
                existing.IsDefault = false;
        }

        var address = new CustomerAddress
        {
            CustomerId = request.CustomerId,
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
