using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.ServiceCategories.Commands.UpdateServiceCategory;

public record UpdateServiceCategoryCommand(
    Guid CategoryId, string Name, string? Description, string? IconOrImageUrl, int DisplayOrder, bool IsActive) : IRequest;

public class UpdateServiceCategoryCommandValidator : AbstractValidator<UpdateServiceCategoryCommand>
{
    public UpdateServiceCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IconOrImageUrl).MaximumLength(500);
    }
}

public class UpdateServiceCategoryCommandHandler : IRequestHandler<UpdateServiceCategoryCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateServiceCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateServiceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new KeyNotFoundException($"ServiceCategory {request.CategoryId} not found.");

        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.IconOrImageUrl = request.IconOrImageUrl;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
