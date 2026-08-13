using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.AddOns.Commands.DeleteAddOn;

public record DeleteAddOnCommand(Guid AddOnId, string? Reason = null) : IRequest;

public class DeleteAddOnCommandHandler : IRequestHandler<DeleteAddOnCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteAddOnCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = await _db.AddOns
            .FirstOrDefaultAsync(a => a.Id == request.AddOnId, cancellationToken)
            ?? throw new KeyNotFoundException($"AddOn {request.AddOnId} not found.");

        // No "in use" guard needed — OrderItemAddOn snapshots Name/Price at order time,
        // so historical orders are unaffected by the add-on later being removed.
        addOn.DeletedReason = request.Reason;
        _db.AddOns.Remove(addOn);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
