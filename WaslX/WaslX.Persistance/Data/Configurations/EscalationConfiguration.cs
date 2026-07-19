using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data.Configurations
{
    public class EscalationConfiguration : IEntityTypeConfiguration<Escalation>
    {
        public void Configure(EntityTypeBuilder<Escalation> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.SuggestedReason)
                .HasMaxLength(1000);

            builder.Property(e => e.OverrideReason)
                .HasMaxLength(1000);

            builder.Property(e => e.ModeAtDecision)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(e => e.Priority)
                .HasMaxLength(20);

            builder.Property(e => e.Sentiment)
                .HasMaxLength(20);

            builder.Property(e => e.EscalationReason)
                .HasMaxLength(1000);

            builder.HasOne(e => e.MessageClassification)
                .WithMany()
                .HasForeignKey(e => e.MessageClassificationId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Conversation)
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AssignedUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.SuggestedAssignee)
                .WithMany()
                .HasForeignKey(e => e.SuggestedAssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ConfirmedByUser)
                .WithMany()
                .HasForeignKey(e => e.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AssignedTo)
                .WithMany()
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
