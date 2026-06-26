using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class CampaignTagConfiguration : IEntityTypeConfiguration<CampaignTag>
    {
        public void Configure(EntityTypeBuilder<CampaignTag> builder)
        {
            builder.ToTable("campaign_tags");
            builder.HasKey(x => new { x.CampaignId, x.TagId });

            builder.Property(x => x.CampaignId).HasColumnName("campaign_id");
            builder.Property(x => x.TagId).HasColumnName("tag_id");

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.CampaignTags)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Tag)
                .WithMany(x => x.CampaignTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
