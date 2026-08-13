using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.AdminSetCustomerPassword;

public record AdminSetCustomerPasswordCommand(Guid CustomerId, string NewPassword) : IRequest;

public class AdminSetCustomerPasswordCommandValidator : AbstractValidator<AdminSetCustomerPasswordCommand>
{
    public AdminSetCustomerPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class AdminSetCustomerPasswordCommandHandler : IRequestHandler<AdminSetCustomerPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IIdentityAuthService _identityAuth;

    public AdminSetCustomerPasswordCommandHandler(IApplicationDbContext db, IIdentityAuthService identityAuth)
    {
        _db = db;
        _identityAuth = identityAuth;
    }

    public async Task Handle(AdminSetCustomerPasswordCommand request, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found.");

        if (customer.IdentityUserId is null)
            throw new InvalidOperationException("This customer has no portal login to set a password for.");

        var succeeded = await _identityAuth.SetPasswordAsync(customer.IdentityUserId.Value, request.NewPassword, cancellationToken);
        if (!succeeded)
            throw new InvalidOperationException("Failed to update password.");
    }
}
