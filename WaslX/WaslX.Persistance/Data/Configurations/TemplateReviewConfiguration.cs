using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Configurations
{
    public class TemplateReviewConfiguration : IEntityTypeConfiguration<TemplateReview>
    {
        public void Configure(EntityTypeBuilder<TemplateReview> builder)
        {
            builder.ToTable("template_reviews");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("template_review_id");
            builder.Property(x => x.MetaTemplateId).HasColumnName("meta_template_id").HasMaxLength(100).IsRequired();
            builder.Property(x => x.MessageTemplateName).HasColumnName("message_template_name").HasMaxLength(512).IsRequired();
            builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(20);
            builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
            builder.Property(x => x.ReasonText).HasColumnName("reason_text").HasMaxLength(1000);
            builder.Property(x => x.MetaNotes).HasColumnName("meta_notes").HasMaxLength(2000);
            builder.Property(x => x.SubmittedCategory).HasColumnName("submitted_category").HasMaxLength(50).IsRequired();
            builder.Property(x => x.AllowCategoryChange).HasColumnName("allow_category_change");
            builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
            
            // New lifecycle fields
            builder.Property(x => x.FinalCategory).HasMaxLength(50);
            builder.Property(x => x.PauseInfo);
            builder.Property(x => x.MetaStatusRaw);
            builder.Property(x => x.DisableTimestamp);
            builder.Property(x => x.DeletedAt);
            
            builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            // One review row per (tenant, Meta template id) — upsert target for create + webhook.
            builder.HasIndex(x => new { x.TenantId, x.MetaTemplateId }).IsUnique();
        }
    }
}
