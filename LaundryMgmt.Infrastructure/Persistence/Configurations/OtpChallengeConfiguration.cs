using LaundryMgmt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaundryMgmt.Infrastructure.Persistence.Configurations;

public class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(o => o.Purpose).HasMaxLength(50).IsRequired();
        builder.Property(o => o.CodeHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(o => new { o.PhoneNumber, o.Purpose, o.IsUsed });
    }
}
