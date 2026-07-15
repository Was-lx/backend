using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class ChannelWhatsAppAccountConfiguration : IEntityTypeConfiguration<ChannelWhatsAppAccount>
    {
        public void Configure(EntityTypeBuilder<ChannelWhatsAppAccount> builder)
        {
            builder.ToTable("channel_whatsapp_accounts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("channel_whatsapp_account_id");
            builder.HasIndex(x => new { x.ChannelId, x.WhatsAppAccountId }).IsUnique();
            builder.HasOne(x => x.Channel).WithMany(x => x.ChannelWhatsAppAccounts).HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.WhatsAppAccount).WithMany(x => x.ChannelWhatsAppAccounts).HasForeignKey(x => x.WhatsAppAccountId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
