using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AiUsageRecordConfiguration : IEntityTypeConfiguration<AiUsageRecord>
    {
        public void Configure(EntityTypeBuilder<AiUsageRecord> builder)
        {
            builder.ToTable("ai_usage_records");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("ai_usage_record_id");
            builder.Property(x => x.Component).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Model).HasMaxLength(100).IsRequired();
            builder.Property(x => x.CostUsd).HasColumnType("decimal(18,6)");

            builder.HasOne(x => x.Tenant)
                .WithMany(x => x.AiUsageRecords)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.CreatedAt });
        }
    }
}
