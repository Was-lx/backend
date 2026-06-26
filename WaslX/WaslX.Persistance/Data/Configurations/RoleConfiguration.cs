using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("role_id");
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        }
    }
}