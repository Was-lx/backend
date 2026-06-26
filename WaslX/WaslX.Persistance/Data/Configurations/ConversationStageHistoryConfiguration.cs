using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ConversationStageHistoryConfiguration : IEntityTypeConfiguration<ConversationStageHistory>
    {
        public void Configure(EntityTypeBuilder<ConversationStageHistory> builder)
        {
            builder.ToTable("conversation_stage_histories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("history_id");
            builder.HasOne(x => x.Conversation).WithMany(x => x.ConversationStageHistories).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Stage).WithMany(x => x.ConversationStageHistories).HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ChangedByUser).WithMany(x => x.ConversationStageHistories).HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}