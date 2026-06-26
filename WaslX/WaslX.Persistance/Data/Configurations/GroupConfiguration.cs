using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("groups");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("group_id");
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
            builder.HasOne(x => x.Tenant).WithMany(x => x.Groups).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}