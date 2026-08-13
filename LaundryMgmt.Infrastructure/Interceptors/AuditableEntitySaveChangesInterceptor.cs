using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LaundryMgmt.Infrastructure.Interceptors;

public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;

    public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUser, IDateTimeProvider dateTime)
    {
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        foreach (EntityEntry<AuditableEntity> entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = _dateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUser.UserName;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = _dateTime.UtcNow;
                    entry.Entity.ModifiedBy = _currentUser.UserName;
                    break;
                case EntityState.Deleted:
                    // Soft delete: convert hard delete into an update.
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = _dateTime.UtcNow;
                    entry.Entity.DeletedBy = _currentUser.UserName;
                    break;
            }
        }
    }
}
