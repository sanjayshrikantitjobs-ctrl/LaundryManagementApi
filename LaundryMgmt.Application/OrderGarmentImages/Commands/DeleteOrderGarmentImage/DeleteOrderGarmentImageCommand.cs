using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.OrderGarmentImages.Commands.DeleteOrderGarmentImage;

public record DeleteOrderGarmentImageCommand(Guid ImageId) : IRequest;

public class DeleteOrderGarmentImageCommandHandler : IRequestHandler<DeleteOrderGarmentImageCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteOrderGarmentImageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteOrderGarmentImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _db.OrderGarmentImages
            .FirstOrDefaultAsync(i => i.Id == request.ImageId, cancellationToken)
            ?? throw new KeyNotFoundException($"Image {request.ImageId} not found.");

        var isAdmin = string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && image.UploadedByUserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You can only delete images you uploaded yourself.");

        _db.OrderGarmentImages.Remove(image);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
