using MediatR;

namespace LaundryMgmt.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity and lightweight
/// domain-event support so aggregates can raise events that get dispatched
/// after SaveChanges (see Infrastructure/Persistence/ApplicationDbContext).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Adds CreatedBy/ModifiedBy/soft-delete tracking. Populated automatically
/// by the SaveChangesInterceptor in Infrastructure.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAtUtc { get; set; }    
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeletedReason { get; set; }
}

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredOnUtc { get; }
}
