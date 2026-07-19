using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Configurations
{
    public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
    {
        public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
        {
            builder.ToTable("knowledge_documents");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("document_id");

            builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Language).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);

            builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
            builder.Property(x => x.FileUrl).HasMaxLength(1000);
            builder.Property(x => x.FileName).HasMaxLength(300);
            builder.Property(x => x.MimeType).HasMaxLength(150);
            builder.Property(x => x.SourceUrl).HasMaxLength(1000);
            builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

            builder.HasOne(x => x.Tenant)
                   .WithMany(x => x.KnowledgeDocuments)
                   .HasForeignKey(x => x.TenantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.SourceType });
        }
    }
}
