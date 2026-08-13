using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Promotions.Commands.UpdatePromotion;

public record UpdatePromotionCommand(
    Guid PromotionId,
    string Title,
    string? Description,
    string? ImageUrl,
    string? Code,
    decimal? DiscountPercent,
    decimal? DiscountAmount,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive) : IRequest;

public class UpdatePromotionCommandValidator : AbstractValidator<UpdatePromotionCommand>
{
    public UpdatePromotionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue);
        RuleFor(x => x.ValidTo)
            .GreaterThanOrEqualTo(x => x.ValidFrom!.Value)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
            .WithMessage("Valid to must be on or after valid from.");
    }
}

public class UpdatePromotionCommandHandler : IRequestHandler<UpdatePromotionCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdatePromotionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await _db.Promotions
            .FirstOrDefaultAsync(p => p.Id == request.PromotionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Promotion {request.PromotionId} not found.");

        var normalizedCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();
        if (normalizedCode is not null &&
            await _db.Promotions.AnyAsync(p => p.Code == normalizedCode && p.Id != request.PromotionId, cancellationToken))
        {
            throw new DomainException($"A promotion with code '{normalizedCode}' already exists.");
        }

        promotion.Title = request.Title.Trim();
        promotion.Description = request.Description;
        promotion.ImageUrl = request.ImageUrl;
        promotion.Code = normalizedCode;
        promotion.DiscountPercent = request.DiscountPercent;
        promotion.DiscountAmount = request.DiscountAmount;
        promotion.ValidFrom = request.ValidFrom;
        promotion.ValidTo = request.ValidTo;
        promotion.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
