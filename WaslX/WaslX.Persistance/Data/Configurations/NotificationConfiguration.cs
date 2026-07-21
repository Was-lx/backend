using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("notification_id");
            builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.EntityType).HasMaxLength(50);

            builder.HasOne(x => x.Tenant).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.User).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.UserId, x.IsRead });
        }
    }
}
