using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
    {
        public void Configure(EntityTypeBuilder<PlatformUser> builder)
        {
            builder.ToTable("platform_users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("platform_user_id");
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.HasIndex(x => x.Email).IsUnique();
        }
    }
}