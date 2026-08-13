using LaundryMgmt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryMgmt.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.SubTotal).HasPrecision(18, 2);
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Property(o => o.GstAmount).HasPrecision(18, 2);
        builder.Property(o => o.DeliveryCharge).HasPrecision(18, 2);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.AmountPaid).HasPrecision(18, 2);
        builder.Property(o => o.PromoCode).HasMaxLength(30);

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(o => o.DomainEvents);
    }
}

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        // Explicitly pinned: the table was created as "OrderStatusHistory" (singular)
        // back when no DbSet existed for it, so EF fell back to the CLR type name.
        // Adding IApplicationDbContext.OrderStatusHistories (plural, matching every
        // other DbSet in this codebase) would otherwise shift EF's naming convention
        // to "OrderStatusHistories" and break every query against the real table.
        builder.ToTable("OrderStatusHistory");
        builder.HasKey(h => h.Id);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);
        builder.Property(i => i.WeightKg).HasPrecision(10, 3);

        builder.HasOne(i => i.Garment).WithMany().HasForeignKey(i => i.GarmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Service).WithMany().HasForeignKey(i => i.ServiceId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.AddOns)
            .WithOne(a => a.OrderItem)
            .HasForeignKey(a => a.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemAddOnConfiguration : IEntityTypeConfiguration<OrderItemAddOn>
{
    public void Configure(EntityTypeBuilder<OrderItemAddOn> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Price).HasPrecision(18, 2);

        builder.HasOne(a => a.AddOn).WithMany().HasForeignKey(a => a.AddOnId).OnDelete(DeleteBehavior.Restrict);
    }
}
