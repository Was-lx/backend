using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Configurations
{
    public class KnowledgeVectorConfiguration : IEntityTypeConfiguration<KnowledgeVector>
    {
        public void Configure(EntityTypeBuilder<KnowledgeVector> builder)
        {
            builder.ToTable("knowledge_vectors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnName("vector_id");

            builder.Property(x => x.SourceType)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(x => x.TextContent)
                   .IsRequired();

            builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            builder.Property(x => x.EmbeddingModel).HasMaxLength(150).IsRequired();

            builder.HasOne(x => x.Tenant)
                   .WithMany(x => x.KnowledgeVectors)
                   .HasForeignKey(x => x.TenantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                   .WithMany(x => x.KnowledgeVectors)
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Document)
                   .WithMany(x => x.Chunks)
                   .HasForeignKey(x => x.DocumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TenantId, x.DocumentId });
            builder.HasIndex(x => new { x.DocumentId, x.ChunkIndex }).IsUnique();
            builder.HasIndex(x => x.QdrantPointId).IsUnique();
        }
    }
}
