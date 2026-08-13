using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.CreateCustomer;

public record CreateCustomerAddressDto(
    string Label, string Line1, string? Line2, string City, string State, string PostalCode, bool IsDefault);

public record CreateCustomerCommand(
    string FullName,
    string PhoneNumber,
    string? Email,
    decimal CreditLimit,
    string? Notes,
    List<CreateCustomerAddressDto>? Addresses) : IRequest<Guid>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Addresses).ChildRules(address =>
        {
            address.RuleFor(a => a.Line1).NotEmpty();
            address.RuleFor(a => a.City).NotEmpty();
            address.RuleFor(a => a.State).NotEmpty();
            address.RuleFor(a => a.PostalCode).NotEmpty();
        });
    }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateCustomerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var normalizedPhone = request.PhoneNumber.Trim();

        var phoneInUse = await _db.Customers.AnyAsync(c => c.PhoneNumber == normalizedPhone, cancellationToken);
        if (phoneInUse)
            throw new InvalidOperationException($"A customer with phone number {normalizedPhone} already exists.");

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = normalizedPhone,
            Email = request.Email?.Trim(),
            CreditLimit = request.CreditLimit,
            Notes = request.Notes
        };

        foreach (var addressDto in request.Addresses ?? new List<CreateCustomerAddressDto>())
        {
            customer.Addresses.Add(new CustomerAddress
            {
                Label = addressDto.Label,
                Line1 = addressDto.Line1,
                Line2 = addressDto.Line2,
                City = addressDto.City,
                State = addressDto.State,
                PostalCode = addressDto.PostalCode,
                IsDefault = addressDto.IsDefault
            });
        }

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
