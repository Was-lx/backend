using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("messages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("message_id");
            builder.Property(x => x.SenderType).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.MessageType).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.WhatsAppMessageId).HasColumnName("wa_message_id").HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.MediaUrl).HasMaxLength(1000);
            builder.Property(x => x.MediaMimeType).HasMaxLength(150);
            builder.Property(x => x.MediaFileName).HasMaxLength(300);
            builder.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.SenderUser).WithMany(x => x.SentMessages).HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
