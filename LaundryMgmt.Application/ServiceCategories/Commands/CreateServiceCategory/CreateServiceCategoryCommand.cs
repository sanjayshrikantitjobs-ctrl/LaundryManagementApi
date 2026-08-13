using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;

namespace LaundryMgmt.Application.ServiceCategories.Commands.CreateServiceCategory;

public record CreateServiceCategoryCommand(
    string Name, string? Description, string? IconOrImageUrl, int DisplayOrder = 0) : IRequest<Guid>;

public class CreateServiceCategoryCommandValidator : AbstractValidator<CreateServiceCategoryCommand>
{
    public CreateServiceCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IconOrImageUrl).MaximumLength(500);
    }
}

public class CreateServiceCategoryCommandHandler : IRequestHandler<CreateServiceCategoryCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateServiceCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateServiceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new ServiceCategory
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            IconOrImageUrl = request.IconOrImageUrl,
            DisplayOrder = request.DisplayOrder
        };

        _db.ServiceCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
