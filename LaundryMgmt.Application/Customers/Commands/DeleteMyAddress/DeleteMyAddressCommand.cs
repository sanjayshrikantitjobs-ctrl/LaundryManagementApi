using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Commands.DeleteMyAddress;

public record DeleteMyAddressCommand(Guid AddressId) : IRequest;

public class DeleteMyAddressCommandHandler : IRequestHandler<DeleteMyAddressCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteMyAddressCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteMyAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        var address = await _db.CustomerAddresses
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == request.AddressId, cancellationToken)
            ?? throw new KeyNotFoundException($"Address {request.AddressId} not found.");

        if (address.Customer?.IdentityUserId != userId)
            throw new UnauthorizedAccessException("You can only manage your own addresses.");

        _db.CustomerAddresses.Remove(address);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
