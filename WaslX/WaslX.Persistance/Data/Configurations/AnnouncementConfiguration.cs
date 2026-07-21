using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
    {
        public void Configure(EntityTypeBuilder<Announcement> builder)
        {
            builder.ToTable("announcements");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("announcement_id");
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            // Body is long-form text.
            builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(x => x.Severity).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Audience).HasMaxLength(30).IsRequired();
            builder.Property(x => x.TargetTenantIds).HasMaxLength(4000);
            builder.Property(x => x.CreatedByPlatformUserId).HasMaxLength(450).IsRequired();
        }
    }
}
