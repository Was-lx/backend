using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class PlatformAuditLogConfiguration : IEntityTypeConfiguration<PlatformAuditLog>
    {
        public void Configure(EntityTypeBuilder<PlatformAuditLog> builder)
        {
            builder.ToTable("platform_audit_logs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("platform_audit_id");
            builder.Property(x => x.ActorPlatformUserId).HasMaxLength(450).IsRequired();
            builder.Property(x => x.ActorEmail).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
            builder.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Details).HasMaxLength(4000);
            builder.Property(x => x.IpAddress).HasMaxLength(64);

            // TenantId is a plain nullable column — deliberately no FK / navigation (global log).
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
