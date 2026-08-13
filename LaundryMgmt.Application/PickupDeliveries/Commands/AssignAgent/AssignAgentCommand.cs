using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.PickupDeliveries.Commands.AssignAgent;

/// <summary>Management assigns a Pickup/Delivery Agent employee to a pickup or
/// delivery leg. Management-only (see AppRoles.ManagementRoles on the controller).</summary>
public record AssignAgentCommand(Guid PickupDeliveryId, Guid EmployeeId) : IRequest;

public class AssignAgentCommandHandler : IRequestHandler<AssignAgentCommand>
{
    private readonly IApplicationDbContext _db;

    public AssignAgentCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(AssignAgentCommand request, CancellationToken cancellationToken)
    {
        var pickupDelivery = await _db.PickupDeliveries
            .FirstOrDefaultAsync(p => p.Id == request.PickupDeliveryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pickup/delivery {request.PickupDeliveryId} not found.");

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee {request.EmployeeId} not found.");

        var expectedRole = pickupDelivery.IsPickup ? UserRole.PickupAgent : UserRole.DeliveryAgent;
        if (employee.Role != expectedRole)
            throw new DomainException($"Employee {employee.FullName} is not a {expectedRole}.");

        pickupDelivery.DeliveryBoyEmployeeId = employee.Id;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
