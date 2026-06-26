using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
    {
        public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
        {
            builder.ToTable("campaign_recipients");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("recipient_id");

            builder.Property(x => x.CampaignId).IsRequired();
            builder.Property(x => x.CustomerId).IsRequired();

            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Error).HasMaxLength(500);

            builder.Property(x => x.SentAt);
            builder.Property(x => x.DeliveredAt);
            builder.Property(x => x.ReadAt);

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.Recipients)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.CampaignRecipients)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
