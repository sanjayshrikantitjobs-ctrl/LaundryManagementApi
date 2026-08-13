using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Application.ServiceCategories.Commands.DeleteServiceCategory;

public record DeleteServiceCategoryCommand(Guid CategoryId, string? Reason = null) : IRequest;

public class DeleteServiceCategoryCommandHandler : IRequestHandler<DeleteServiceCategoryCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteServiceCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteServiceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new KeyNotFoundException($"ServiceCategory {request.CategoryId} not found.");

        // Service.CategoryId's Restrict FK can't protect against this at the DB level —
        // soft-delete turns the Remove() below into an UPDATE (IsDeleted=true), not a
        // real DELETE, so the FK constraint never fires. Guard explicitly instead.
        var hasServices = await _db.Services.AnyAsync(s => s.CategoryId == request.CategoryId, cancellationToken);
        if (hasServices)
            throw new DomainException("Reassign or remove this category's services before deleting it.");

        var hasGarments = await _db.Garments.AnyAsync(g => g.CategoryId == request.CategoryId, cancellationToken);
        if (hasGarments)
            throw new DomainException("Reassign or remove this category's garments before deleting it.");

        category.DeletedReason = request.Reason;
        _db.ServiceCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
