using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Application.Common.Models;
using LaundryMgmt.Domain.Enums;
using MediatR;

namespace LaundryMgmt.Application.Users.Queries.GetUsers;

public record GetUsersQuery(
    string? Search = null,
    UserRole? Role = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null) : IRequest<PaginatedList<UserSummaryDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserSummaryDto>>
{
    private readonly IUserManagementService _userManagement;

    public GetUsersQueryHandler(IUserManagementService userManagement) => _userManagement = userManagement;

    public async Task<PaginatedList<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _userManagement.GetUsersAsync(
            request.Search, request.Role, request.IsActive, request.PageNumber, request.PageSize,
            request.SortBy, request.SortDirection, cancellationToken);

        return new PaginatedList<UserSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
