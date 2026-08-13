using FluentValidation;
using LaundryMgmt.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.Services.Commands.UpdateService;

public record UpdateServiceCommand(
    Guid ServiceId, string Name, Guid CategoryId, decimal BasePrice, int EstimatedTimeHours, decimal GstPercentage, int Priority,
    string? ImageUrl = null, decimal ExpressSurcharge = 0, int ExpressEtaHours = 24) : IRequest;

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
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

public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateServiceCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service {request.ServiceId} not found.");

        service.Name = request.Name.Trim();
        service.CategoryId = request.CategoryId;
        service.BasePrice = request.BasePrice;
        service.EstimatedTimeHours = request.EstimatedTimeHours;
        service.GstPercentage = request.GstPercentage;
        service.Priority = request.Priority;
        service.ImageUrl = request.ImageUrl;
        service.ExpressSurcharge = request.ExpressSurcharge;
        service.ExpressEtaHours = request.ExpressEtaHours;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
