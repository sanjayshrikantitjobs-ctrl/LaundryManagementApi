using LaundryMgmt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryMgmt.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(18, 2);

        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasMany(p => p.Features)
            .WithOne(f => f.SubscriptionPlan)
            .HasForeignKey(f => f.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);
    }
}

public class SubscriptionPlanFeatureConfiguration : IEntityTypeConfiguration<SubscriptionPlanFeature>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanFeature> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Text).HasMaxLength(200).IsRequired();
    }
}

public class CustomerSubscriptionConfiguration : IEntityTypeConfiguration<CustomerSubscription>
{
    public void Configure(EntityTypeBuilder<CustomerSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RecurringValue).HasPrecision(18, 2);
        builder.Property(s => s.Notes).HasMaxLength(500);

        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(s => s.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(s => s.DomainEvents);
    }
}
