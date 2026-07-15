using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AgentShiftConfiguration : IEntityTypeConfiguration<AgentShift>
    {
        public void Configure(EntityTypeBuilder<AgentShift> builder)
        {
            builder.ToTable("agent_shifts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("agent_shift_id");
            builder.HasIndex(x => new { x.UserId, x.ShiftId }).IsUnique();
            builder.HasOne(x => x.User).WithMany(x => x.AgentShifts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Shift).WithMany(x => x.AgentShifts).HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
