using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedByIp)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.RevokedByIp)
            .HasMaxLength(64);

        builder.Property(x => x.ReplacedByTokenHash)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAtUtc);
        // TD-04: composite index to make the reuse-detection query (UserId + RevokedAtUtc IS NULL)
        // sargable — previously only a single-column UserId index existed.
        builder.HasIndex(x => new { x.UserId, x.RevokedAtUtc })
            .HasFilter("[RevokedAtUtc] IS NULL")
            .HasDatabaseName("IX_RefreshTokens_UserId_Active");
    }
}