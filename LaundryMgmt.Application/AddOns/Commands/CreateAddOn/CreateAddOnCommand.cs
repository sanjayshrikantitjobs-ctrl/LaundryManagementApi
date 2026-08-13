using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;

namespace LaundryMgmt.Application.AddOns.Commands.CreateAddOn;

public record CreateAddOnCommand(string Name, string? Description, decimal Price) : IRequest<Guid>;

public class CreateAddOnCommandValidator : AbstractValidator<CreateAddOnCommand>
{
    public CreateAddOnCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class CreateAddOnCommandHandler : IRequestHandler<CreateAddOnCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateAddOnCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = new AddOn
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price
        };

        _db.AddOns.Add(addOn);
        await _db.SaveChangesAsync(cancellationToken);

        return addOn.Id;
    }
}
