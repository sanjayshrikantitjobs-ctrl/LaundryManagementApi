using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.SetCustomerStatus;

public record SetCustomerStatusCommand(Guid CustomerId, CustomerStatus Status) : IRequest;

public class SetCustomerStatusCommandHandler : IRequestHandler<SetCustomerStatusCommand>
{
    private readonly IApplicationDbContext _db;

    public SetCustomerStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(SetCustomerStatusCommand request, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found.");

        // Only flips the status flag — orders, addresses, invoices and wallet
        // history are untouched, so deactivating a customer never loses data.
        customer.Status = request.Status;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
