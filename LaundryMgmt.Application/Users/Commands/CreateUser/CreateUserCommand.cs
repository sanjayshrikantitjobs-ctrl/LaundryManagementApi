using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Enums;
using MediatR;

namespace LaundryMgmt.Application.Users.Commands.CreateUser;

public record CreateUserCommand(
    string FullName,
    string UserName,
    string? Email,
    string? PhoneNumber,
    string Password,
    UserRole Role) : IRequest<Guid>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
        // Identity is configured with RequireUniqueEmail = true, so every login needs one.
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).IsInEnum().NotEqual(UserRole.Customer)
            .WithMessage("Customer accounts are created via self-registration, not User Management.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserManagementService _userManagement;
    private readonly IApplicationDbContext _db;

    public CreateUserCommandHandler(IUserManagementService userManagement, IApplicationDbContext db)
    {
        _userManagement = userManagement;
        _db = db;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userId = await _userManagement.CreateUserAsync(
            request.FullName.Trim(), request.UserName.Trim(), request.Email?.Trim(),
            request.PhoneNumber?.Trim(), request.Password, request.Role, cancellationToken);

        // Pickup/Delivery agents need a linked Employee row so their assigned
        // pickups/deliveries can be resolved back to "orders assigned to me" —
        // mirrors how self-registration links a Customer row to the login.
        if (request.Role is UserRole.PickupAgent or UserRole.DeliveryAgent or UserRole.DepartmentHead)
        {
            _db.Employees.Add(new Employee
            {
                FullName = request.FullName.Trim(),
                Role = request.Role,
                JoinedOn = DateOnly.FromDateTime(DateTime.UtcNow),
                IdentityUserId = userId
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return userId;
    }
}
