using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data.Configurations
{
    public class TenantEscalationSettingsConfiguration : IEntityTypeConfiguration<TenantEscalationSettings>
    {
        public void Configure(EntityTypeBuilder<TenantEscalationSettings> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Mode)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

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
