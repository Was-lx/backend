using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
    {
        public void Configure(EntityTypeBuilder<Shift> builder)
        {
            builder.ToTable("shifts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("shift_id");
            builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
            builder.HasOne(x => x.Tenant).WithMany(x => x.Shifts).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
