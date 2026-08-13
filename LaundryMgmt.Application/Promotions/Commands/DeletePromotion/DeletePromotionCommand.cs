using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Promotions.Commands.DeletePromotion;

public record DeletePromotionCommand(Guid PromotionId, string? Reason = null) : IRequest;

public class DeletePromotionCommandHandler : IRequestHandler<DeletePromotionCommand>
{
    private readonly IApplicationDbContext _db;

    public DeletePromotionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeletePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(p => p.Id == request.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion {request.PromotionId} not found.");

        promotion.DeletedReason = request.Reason;
        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
