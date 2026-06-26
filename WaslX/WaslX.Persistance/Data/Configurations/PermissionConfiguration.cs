using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("permissions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("permission_id");
            builder.Property(x => x.Code).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
        }
    }
}