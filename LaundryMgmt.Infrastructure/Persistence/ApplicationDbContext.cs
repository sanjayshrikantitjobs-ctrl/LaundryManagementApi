using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Domain.Common;
using LaundryMgmt.Domain.Entities;
using LaundryMgmt.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LaundryMgmt.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly IPublisher? _publisher;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher? publisher = null)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Garment> Garments => Set<Garment>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<GarmentServicePrice> GarmentServicePrices => Set<GarmentServicePrice>();
    public DbSet<AddOn> AddOns => Set<AddOn>();
    public DbSet<OrderItemAddOn> OrderItemAddOns => Set<OrderItemAddOn>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<PickupDelivery> PickupDeliveries => Set<PickupDelivery>();
    public DbSet<OrderGarmentImage> OrderGarmentImages => Set<OrderGarmentImage>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures => Set<SubscriptionPlanFeature>();
    public DbSet<CustomerSubscription> CustomerSubscriptions => Set<CustomerSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // sets up AspNetUsers/Roles/etc. first

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global soft-delete filter for every AuditableEntity.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
                var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // NOTE: CreatedAtUtc/ModifiedAtUtc/CreatedBy stamping happens in
        // AuditableEntitySaveChangesInterceptor (registered in DependencyInjection.cs),
        // kept separate so this method stays focused on persistence + events.
        var result = await base.SaveChangesAsync(cancellationToken);

        if (_publisher is not null)
        {
            var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count != 0)
                .ToList();

            foreach (var entity in entitiesWithEvents)
            {
                var events = entity.DomainEvents.ToArray();
                entity.ClearDomainEvents();
                foreach (var domainEvent in events)
                    await _publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return result;
    }
}
