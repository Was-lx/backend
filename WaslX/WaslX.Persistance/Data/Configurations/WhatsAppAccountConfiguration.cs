using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class WhatsAppAccountConfiguration : IEntityTypeConfiguration<WhatsAppAccount>
    {
        public void Configure(EntityTypeBuilder<WhatsAppAccount> builder)
        {
            builder.ToTable("whatsapp_accounts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("wa_account_id");
            builder.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            builder.Property(x => x.AccessToken).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Tenant).WithMany(x => x.WhatsAppAccounts).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
