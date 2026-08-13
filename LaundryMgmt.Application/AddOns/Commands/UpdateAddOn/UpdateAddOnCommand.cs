using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.AddOns.Commands.UpdateAddOn;

public record UpdateAddOnCommand(Guid AddOnId, string Name, string? Description, decimal Price, bool IsActive) : IRequest;

public class UpdateAddOnCommandValidator : AbstractValidator<UpdateAddOnCommand>
{
    public UpdateAddOnCommandValidator()
    {
        RuleFor(x => x.AddOnId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class UpdateAddOnCommandHandler : IRequestHandler<UpdateAddOnCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateAddOnCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateAddOnCommand request, CancellationToken cancellationToken)
    {
        var addOn = await _db.AddOns
            .FirstOrDefaultAsync(a => a.Id == request.AddOnId, cancellationToken)
            ?? throw new KeyNotFoundException($"AddOn {request.AddOnId} not found.");

        addOn.Name = request.Name.Trim();
        addOn.Description = request.Description;
        addOn.Price = request.Price;
        addOn.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
