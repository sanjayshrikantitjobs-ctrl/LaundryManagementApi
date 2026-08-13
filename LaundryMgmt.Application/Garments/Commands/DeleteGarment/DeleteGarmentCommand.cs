using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Garments.Commands.DeleteGarment;

public record DeleteGarmentCommand(Guid GarmentId, string? Reason = null) : IRequest;

public class DeleteGarmentCommandHandler : IRequestHandler<DeleteGarmentCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteGarmentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteGarmentCommand request, CancellationToken cancellationToken)
    {
        var garment = await _db.Garments
            .FirstOrDefaultAsync(g => g.Id == request.GarmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Garment {request.GarmentId} not found.");

        garment.DeletedReason = request.Reason;
        _db.Garments.Remove(garment);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
