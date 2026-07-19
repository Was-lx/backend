using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data.Configurations;

public class MessageClassificationConfiguration : IEntityTypeConfiguration<MessageClassification>
{
    public void Configure(EntityTypeBuilder<MessageClassification> builder)
    {
        builder.ToTable("MessageClassifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Topic).HasMaxLength(50);
        builder.Property(x => x.Language).HasMaxLength(50);
        builder.Property(x => x.Sentiment).HasMaxLength(50);
        builder.Property(x => x.Priority).HasMaxLength(50);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.ClassifierVersion).HasMaxLength(100);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Conversation)
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Message)
            .WithMany()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.NoAction);

        // Constraints: one classification per message
        builder.HasIndex(x => x.MessageId).IsUnique();

        // indexes on TenantId, ConversationId, MessageId, Escalate, Priority, Sentiment
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.Escalate);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.Sentiment);
    }
}
