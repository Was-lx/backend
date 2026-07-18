using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ConversationSummaryConfiguration : IEntityTypeConfiguration<ConversationSummary>
    {
        public void Configure(EntityTypeBuilder<ConversationSummary> builder)
        {
            builder.ToTable("conversation_summaries");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("conversation_summary_id");

            builder.Property(x => x.ShortSummary).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.FullSummary); // nvarchar(max)

            // One cached summary per conversation (upserted as the thread grows).
            builder.HasIndex(x => x.ConversationId).IsUnique();

            builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
