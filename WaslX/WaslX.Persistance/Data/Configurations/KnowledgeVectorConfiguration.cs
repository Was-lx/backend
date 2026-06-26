using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

            builder.Property(x => x.TextContent)
                   .IsRequired();

            builder.Property(x => x.Embedding)
                .HasConversion(
                    v => string.Join(',', v),
                    v => string.IsNullOrWhiteSpace(v)
                        ? Array.Empty<float>()
                        : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(float.Parse)
                           .ToArray())
                .Metadata.SetValueComparer(
                    new ValueComparer<float[]>(
                        (a, b) => a!.SequenceEqual(b!),
                        v => v.Aggregate(0, (hash, value) => HashCode.Combine(hash, value)),
                        v => v.ToArray()
                    ));
            builder.HasOne(x => x.Tenant)
                   .WithMany(x => x.KnowledgeVectors)
                   .HasForeignKey(x => x.TenantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                   .WithMany(x => x.KnowledgeVectors)
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
