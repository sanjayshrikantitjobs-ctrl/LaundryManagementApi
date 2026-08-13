using LaundryMgmt.Application.Common.Interfaces;
using MediatR;

namespace LaundryMgmt.Application.Users.Commands.SetUserActive;

public record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest;

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IUserManagementService _userManagement;

    public SetUserActiveCommandHandler(IUserManagementService userManagement) => _userManagement = userManagement;

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        await _userManagement.SetActiveAsync(request.UserId, request.IsActive, cancellationToken);
    }
}
