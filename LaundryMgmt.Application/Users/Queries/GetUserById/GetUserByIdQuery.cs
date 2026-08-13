using LaundryMgmt.Application.Common.Interfaces;
using MediatR;

namespace LaundryMgmt.Application.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserSummaryDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserSummaryDto>
{
    private readonly IUserManagementService _userManagement;

    public GetUserByIdQueryHandler(IUserManagementService userManagement) => _userManagement = userManagement;

    public async Task<UserSummaryDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _userManagement.GetUserByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");
    }
}
