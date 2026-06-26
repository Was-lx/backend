using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
    {
        public void Configure(EntityTypeBuilder<Campaign> builder)
        {
            builder.ToTable("campaigns");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("campaign_id");

            builder.Property(x => x.WaAccountId).IsRequired();

            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.TemplateName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.MessageBody).HasMaxLength(4000).IsRequired();
            builder.Property(x => x.AudienceFilter).HasMaxLength(1000);
            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();

            builder.Property(x => x.SentCount).HasDefaultValue(0);
            builder.Property(x => x.DeliveredCount).HasDefaultValue(0);
            builder.Property(x => x.ReadCount).HasDefaultValue(0);
            builder.Property(x => x.FailedCount).HasDefaultValue(0);

            builder.HasOne(x => x.Tenant)
                .WithMany(x => x.Campaigns)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.WhatsAppAccount)
                .WithMany(x => x.Campaigns)
                .HasForeignKey(x => x.WaAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Creator)
                .WithMany(x => x.CreatedCampaigns)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
