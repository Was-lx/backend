using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ImpersonationSessionConfiguration : IEntityTypeConfiguration<ImpersonationSession>
    {
        public void Configure(EntityTypeBuilder<ImpersonationSession> builder)
        {
            builder.ToTable("impersonation_sessions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("impersonation_session_id");
            builder.Property(x => x.ActorPlatformUserId).HasMaxLength(450).IsRequired();
            builder.Property(x => x.ActorEmail).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            // Status stored as string (Active/Ended/Expired).
            builder.Property(x => x.Status).HasMaxLength(20).IsRequired();

            // TenantId is a plain int column — deliberately no FK / navigation (platform-level log).
            builder.HasIndex(x => x.TenantId);
            builder.HasIndex(x => x.Status);
        }
    }
}
