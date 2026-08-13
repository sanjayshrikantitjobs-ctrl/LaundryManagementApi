using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Entities;
using MediatR;

namespace LaundryMgmt.Application.Services.Commands.CreateService;

public record CreateServiceCommand(
    string Name, Guid CategoryId, decimal BasePrice, int EstimatedTimeHours, decimal GstPercentage, int Priority,
    string? ImageUrl = null, decimal ExpressSurcharge = 0, int ExpressEtaHours = 24) : IRequest<Guid>;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimatedTimeHours).GreaterThan(0);
        RuleFor(x => x.GstPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.ExpressSurcharge).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpressEtaHours).GreaterThan(0);
    }
}

public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateServiceCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = new Service
        {
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            BasePrice = request.BasePrice,
            EstimatedTimeHours = request.EstimatedTimeHours,
            GstPercentage = request.GstPercentage,
            Priority = request.Priority,
            ImageUrl = request.ImageUrl,
            ExpressSurcharge = request.ExpressSurcharge,
            ExpressEtaHours = request.ExpressEtaHours
        };

        _db.Services.Add(service);
        await _db.SaveChangesAsync(cancellationToken);

        return service.Id;
    }
}
