using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data.Configurations
{
    public class TenantAiAgentSettingsConfiguration : IEntityTypeConfiguration<TenantAiAgentSettings>
    {
        public void Configure(EntityTypeBuilder<TenantAiAgentSettings> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.PersonaName).HasMaxLength(100);
            builder.Property(s => s.ToneInstructions).HasMaxLength(1000);
            builder.Property(s => s.HandoffThreshold).HasColumnType("decimal(3,2)");

            builder.HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.UpdatedByUser)
                .WithMany()
                .HasForeignKey(s => s.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(s => s.TenantId)
                .IsUnique();
        }
    }
}
