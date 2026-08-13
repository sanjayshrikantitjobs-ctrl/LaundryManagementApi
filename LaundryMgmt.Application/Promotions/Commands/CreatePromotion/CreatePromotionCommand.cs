using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Promotions.Commands.CreatePromotion;

public record CreatePromotionCommand(
    string Title,
    string? Description,
    string? ImageUrl,
    string? Code,
    decimal? DiscountPercent,
    decimal? DiscountAmount,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive = true) : IRequest<Guid>;

public class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
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

public class CreatePromotionCommandHandler : IRequestHandler<CreatePromotionCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreatePromotionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();

        if (normalizedCode is not null && await _db.Promotions.AnyAsync(p => p.Code == normalizedCode, cancellationToken))
            throw new DomainException($"A promotion with code '{normalizedCode}' already exists.");

        var promotion = new Promotion
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Code = normalizedCode,
            DiscountPercent = request.DiscountPercent,
            DiscountAmount = request.DiscountAmount,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = request.IsActive
        };

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync(cancellationToken);

        return promotion.Id;
    }
}
