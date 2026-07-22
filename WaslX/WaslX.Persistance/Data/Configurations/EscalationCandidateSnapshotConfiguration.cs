using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data.Configurations
{
    public class EscalationCandidateSnapshotConfiguration : IEntityTypeConfiguration<EscalationCandidateSnapshot>
    {
        public void Configure(EntityTypeBuilder<EscalationCandidateSnapshot> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.AgentName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.OverallScore)
                .HasPrecision(10, 4);

            builder.Property(e => e.PerformanceScore)
                .HasPrecision(10, 4);

            builder.Property(e => e.ResponseSpeedScore)
                .HasPrecision(10, 4);

            builder.Property(e => e.WorkloadScore)
                .HasPrecision(10, 4);

            builder.Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Reason)
                .HasMaxLength(500);

            builder.HasOne(e => e.Escalation)
                .WithMany(e => e.CandidateSnapshots)
                .HasForeignKey(e => e.EscalationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.EscalationId);
        }
    }
}
