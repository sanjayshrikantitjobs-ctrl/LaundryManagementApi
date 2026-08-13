using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Services.Commands.DeleteService;

public record DeleteServiceCommand(Guid ServiceId, string? Reason = null) : IRequest;

public class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteServiceCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service {request.ServiceId} not found.");

        service.DeletedReason = request.Reason;
        _db.Services.Remove(service);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
