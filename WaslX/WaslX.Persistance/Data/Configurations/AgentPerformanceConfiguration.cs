using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AgentPerformanceConfiguration : IEntityTypeConfiguration<AgentPerformance>
    {
        public void Configure(EntityTypeBuilder<AgentPerformance> builder)
        {
            builder.ToTable("agent_performances");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("performance_id");
            builder.Property(x => x.AvgResponseTime).HasPrecision(18, 2);
            builder.Property(x => x.ResolutionRate).HasPrecision(18, 2);
            builder.HasOne(x => x.User).WithMany(x => x.AgentPerformances).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
