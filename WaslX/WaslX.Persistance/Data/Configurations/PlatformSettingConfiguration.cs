using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
    {
        public void Configure(EntityTypeBuilder<PlatformSetting> builder)
        {
            builder.ToTable("platform_settings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("platform_setting_id");
            builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.ValueType).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);

            builder.HasIndex(x => x.Key).IsUnique();
        }
    }
}
