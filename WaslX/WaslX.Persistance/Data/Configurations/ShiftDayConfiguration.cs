using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ShiftDayConfiguration : IEntityTypeConfiguration<ShiftDay>
    {
        public void Configure(EntityTypeBuilder<ShiftDay> builder)
        {
            builder.ToTable("shift_days");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("shift_day_id");
            builder.Property(x => x.DayOfWeek).HasConversion<int>();
            builder.HasIndex(x => new { x.ShiftId, x.DayOfWeek }).IsUnique();
            builder.HasOne(x => x.Shift).WithMany(x => x.ShiftDays).HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
