using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data.Configurations;

internal sealed class TemplateReviewHistoryConfiguration : IEntityTypeConfiguration<TemplateReviewHistory>
{
    public void Configure(EntityTypeBuilder<TemplateReviewHistory> builder)
    {
        builder.ToTable("TemplateReviewHistories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ReasonCode).HasMaxLength(50);
        builder.Property(x => x.ReasonText).HasMaxLength(500);
        builder.Property(x => x.FinalCategory).HasMaxLength(50);

        // Raw JSON payloads
        builder.Property(x => x.PauseInfo);
        builder.Property(x => x.MetaStatusRaw);

        // Navigation
        builder.HasOne(x => x.TemplateReview)
            .WithMany()
            .HasForeignKey(x => x.TemplateReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.TemplateReviewId);
        builder.HasIndex(x => x.TenantId);
    }
}
