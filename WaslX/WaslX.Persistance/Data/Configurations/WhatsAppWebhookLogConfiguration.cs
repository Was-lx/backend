using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class WhatsAppWebhookLogConfiguration : IEntityTypeConfiguration<WhatsAppWebhookLog>
    {
        public void Configure(EntityTypeBuilder<WhatsAppWebhookLog> builder)
        {
            builder.ToTable("whatsapp_webhook_logs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("wa_webhook_log_id");
            builder.Property(x => x.RawPayload).HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(x => x.EventType).HasMaxLength(50).IsRequired();
            builder.Property(x => x.PhoneNumberId).HasMaxLength(30);
            builder.Property(x => x.ProcessingError).HasMaxLength(2000);
            builder.HasIndex(x => x.PhoneNumberId);
            builder.HasIndex(x => x.Processed);
        }
    }
}
