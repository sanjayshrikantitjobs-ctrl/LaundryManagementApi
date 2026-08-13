using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.SetPrimaryAddress;

public record SetPrimaryAddressCommand(Guid AddressId) : IRequest;

public class SetPrimaryAddressCommandHandler : IRequestHandler<SetPrimaryAddressCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetPrimaryAddressCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SetPrimaryAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        var address = await _db.CustomerAddresses
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == request.AddressId, cancellationToken)
            ?? throw new KeyNotFoundException($"Address {request.AddressId} not found.");

        if (address.Customer?.IdentityUserId != userId)
            throw new UnauthorizedAccessException("You can only manage your own addresses.");

        var siblings = await _db.CustomerAddresses
            .Where(a => a.CustomerId == address.CustomerId && a.IsDefault)
            .ToListAsync(cancellationToken);
        foreach (var sibling in siblings)
            sibling.IsDefault = false;

        address.IsDefault = true;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
