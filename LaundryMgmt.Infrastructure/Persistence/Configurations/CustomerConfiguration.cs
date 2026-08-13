using LaundryMgmt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryMgmt.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.WhatsAppNumber).HasMaxLength(20);

        builder.Property(c => c.WalletBalance).HasPrecision(18, 2);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);

        builder.HasIndex(c => c.PhoneNumber).IsUnique();
        builder.HasIndex(c => c.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");

        builder.HasMany(c => c.Addresses)
            .WithOne(a => a.Customer)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);
    }
}

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Label).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Line1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();

        builder.Ignore(a => a.DomainEvents);
    }
}
