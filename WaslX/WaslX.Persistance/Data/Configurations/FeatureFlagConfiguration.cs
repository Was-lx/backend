using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
    {
        public void Configure(EntityTypeBuilder<FeatureFlag> builder)
        {
            builder.ToTable("feature_flags");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("feature_flag_id");
            builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);

            // Unique per (Key, TenantId): one global null-tenant row + per-tenant overrides.
            // TenantId is a plain nullable column — deliberately no FK / navigation.
            builder.HasIndex(x => new { x.Key, x.TenantId }).IsUnique();
        }
    }
}
