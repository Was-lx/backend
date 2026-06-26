using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("tags");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("tag_id");
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Color).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
            builder.HasOne(x => x.Tenant).WithMany(x => x.Tags).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
