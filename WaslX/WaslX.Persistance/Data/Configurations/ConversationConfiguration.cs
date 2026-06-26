using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.ToTable("conversations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("conversation_id");
            builder.Property(x => x.WhatsAppAccountId).HasColumnName("wa_account_id");
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Tenant).WithMany(x => x.Conversations).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.WhatsAppAccount).WithMany(x => x.Conversations).HasForeignKey(x => x.WhatsAppAccountId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Customer).WithMany(x => x.Conversations).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.AssignedUser).WithMany(x => x.AssignedConversations).HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Group).WithMany(x => x.Conversations).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.CurrentStage).WithMany(x => x.CurrentConversations).HasForeignKey(x => x.CurrentStageId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}