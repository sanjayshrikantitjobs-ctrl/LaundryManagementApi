using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Customers.Queries.GetMyAddresses;

public record MyAddressDto(
    Guid Id, string Label, string Line1, string? Line2, string City, string State, string PostalCode, bool IsDefault);

public record GetMyAddressesQuery : IRequest<List<MyAddressDto>>;

public class GetMyAddressesQueryHandler : IRequestHandler<GetMyAddressesQuery, List<MyAddressDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyAddressesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<MyAddressDto>> Handle(GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not signed in.");

        return await _db.CustomerAddresses
            .Where(a => a.Customer!.IdentityUserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Label)
            .Select(a => new MyAddressDto(a.Id, a.Label, a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.IsDefault))
            .ToListAsync(cancellationToken);
    }
}
