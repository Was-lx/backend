using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("audit_id");
            builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Details).IsRequired();
            builder.HasOne(x => x.Tenant).WithMany(x => x.AuditLogs).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ActorUser).WithMany(x => x.AuditLogs).HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}