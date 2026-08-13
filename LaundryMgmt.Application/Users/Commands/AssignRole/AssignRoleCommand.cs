using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Enums;
using MediatR;

namespace LaundryMgmt.Application.Users.Commands.AssignRole;

public record AssignRoleCommand(Guid UserId, UserRole Role) : IRequest;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.Role).IsInEnum().NotEqual(UserRole.Customer)
            .WithMessage("Customer accounts can't be reassigned to a staff role here.");
    }
}

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IUserManagementService _userManagement;

    public AssignRoleCommandHandler(IUserManagementService userManagement) => _userManagement = userManagement;

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        await _userManagement.AssignRoleAsync(request.UserId, request.Role, cancellationToken);
    }
}
