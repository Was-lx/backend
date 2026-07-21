using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class BudgetAlertConfiguration : IEntityTypeConfiguration<BudgetAlert>
    {
        public void Configure(EntityTypeBuilder<BudgetAlert> builder)
        {
            builder.ToTable("budget_alerts");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("budget_alert_id");
            builder.Property(x => x.Scope).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ThresholdUsd).HasColumnType("decimal(18,6)");
            builder.Property(x => x.Period).HasMaxLength(50).IsRequired();
            builder.Property(x => x.NotifyEmail).HasMaxLength(256);

            // TenantId is a plain nullable column — null = global alert. Deliberately no FK / navigation.
        }
    }
}
