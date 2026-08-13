using LaundryMgmt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryMgmt.Infrastructure.Persistence.Configurations;

public class PickupDeliveryConfiguration : IEntityTypeConfiguration<PickupDelivery>
{
    public void Configure(EntityTypeBuilder<PickupDelivery> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Charge).HasPrecision(18, 2);
        builder.Property(p => p.Otp).HasMaxLength(10);

        builder.HasOne(p => p.Address)
            .WithMany()
            .HasForeignKey(p => p.AddressId)
            .OnDelete(DeleteBehavior.SetNull);

        // Without this, EF Core can't match DeliveryBoyEmployeeId to the DeliveryBoy
        // navigation by convention (the names don't fit any of EF's naming patterns)
        // and instead creates its own shadow FK column, silently orphaning the real
        // one — so DeliveryBoyEmployeeId gets written correctly but DeliveryBoy is
        // never populated on read.
        builder.HasOne(p => p.DeliveryBoy)
            .WithMany()
            .HasForeignKey(p => p.DeliveryBoyEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(p => p.DomainEvents);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Shift).HasMaxLength(50);
        builder.Property(e => e.Salary).HasPrecision(18, 2);

        builder.HasIndex(e => e.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");

        builder.Ignore(e => e.DomainEvents);
    }
}

public class OrderGarmentImageConfiguration : IEntityTypeConfiguration<OrderGarmentImage>
{
    public void Configure(EntityTypeBuilder<OrderGarmentImage> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(i => i.UploadedByName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.HasOne(i => i.Order)
            .WithMany()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Service)
            .WithMany()
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.OrderItem)
            .WithMany()
            .HasForeignKey(i => i.OrderItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => new { i.OrderId, i.ImageType });

        builder.Ignore(i => i.DomainEvents);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientRole).HasMaxLength(30);
        builder.Property(n => n.Title).HasMaxLength(150).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();
        builder.Property(n => n.EntityId).HasMaxLength(64);

        builder.HasIndex(n => new { n.RecipientRole, n.IsRead });
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });

        builder.Ignore(n => n.DomainEvents);
    }
}

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Message).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.ImageUrl).HasMaxLength(500);
        builder.Property(c => c.Response).HasMaxLength(2000);

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.DomainEvents);
    }
}
