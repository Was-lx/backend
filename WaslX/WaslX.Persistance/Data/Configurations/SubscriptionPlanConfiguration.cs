using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.ToTable("subscription_plans");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("plan_id");
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Price).HasPrecision(18, 2);
            builder.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(50);
        }
    }
}
