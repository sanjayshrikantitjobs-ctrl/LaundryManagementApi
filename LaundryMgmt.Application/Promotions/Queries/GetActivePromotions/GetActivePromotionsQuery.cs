using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Promotions.Queries.GetActivePromotions;

public record ActivePromotionDto(
    Guid Id, string Title, string? Description, string? ImageUrl, string? Code,
    decimal? DiscountPercent, decimal? DiscountAmount, DateTimeOffset? ValidTo);

/// <summary>Customer-facing listing — only promotions marked active and currently within
/// their validity window (if one is set), for the "Promotions & Offers" page.</summary>
public record GetActivePromotionsQuery : IRequest<List<ActivePromotionDto>>;

public class GetActivePromotionsQueryHandler : IRequestHandler<GetActivePromotionsQuery, List<ActivePromotionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _dateTime;

    public GetActivePromotionsQueryHandler(IApplicationDbContext db, IDateTimeProvider dateTime)
    {
        _db = db;
        _dateTime = dateTime;
    }

    public async Task<List<ActivePromotionDto>> Handle(GetActivePromotionsQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        return await _db.Promotions
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new ActivePromotionDto(
                p.Id, p.Title, p.Description, p.ImageUrl, p.Code, p.DiscountPercent, p.DiscountAmount, p.ValidTo))
            .ToListAsync(cancellationToken);
    }
}
