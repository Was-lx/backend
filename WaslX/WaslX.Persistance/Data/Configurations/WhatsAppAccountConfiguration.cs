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
            builder.Property(x => x.AccessToken).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Tenant).WithMany(x => x.WhatsAppAccounts).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.PhoneNumberId).HasMaxLength(20).IsRequired();
            // Keep the original column name so the rename to PascalCase needs no migration.
            builder.Property(x => x.WhatsAppBusinessAccountId).HasColumnName("whatsAppBusinessAccountId").HasMaxLength(20).IsRequired();

            // ── Sprint 3: distribution config ──
            builder.Property(x => x.PlatformName).HasMaxLength(120);
            // Stored as the enum name. New rows always get a valid value from the entity default
            // (RoundRobin); legacy rows that were back-filled with "" are healed by a data migration
            // (see the UPDATE run against whatsapp_accounts). No HasDefaultValue here — it would make
            // ByAdmin (CLR default 0) unsettable due to EF's sentinel rule.
            builder.Property(x => x.DistributionMode).HasConversion<string>().HasMaxLength(40);
            builder.HasOne(x => x.StartingGroup).WithMany().HasForeignKey(x => x.StartingGroupId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
