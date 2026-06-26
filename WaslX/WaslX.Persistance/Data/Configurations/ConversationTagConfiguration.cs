using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ConversationTagConfiguration : IEntityTypeConfiguration<ConversationTag>
    {
        public void Configure(EntityTypeBuilder<ConversationTag> builder)
        {
            builder.ToTable("conversation_tags");
            builder.HasKey(x => new { x.ConversationId, x.TagId });
            builder.HasOne(x => x.Conversation).WithMany(x => x.ConversationTags).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Tag).WithMany(x => x.ConversationTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}