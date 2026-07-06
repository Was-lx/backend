using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
            builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Tagline).HasMaxLength(300);
            builder.Property(x => x.Price).HasPrecision(18, 2);
            builder.Property(x => x.PriceYearly).HasPrecision(18, 2);
            builder.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(50);

            // Display bullet list stored as a JSON string column.
            builder.Property(x => x.Features)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                    v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s == null ? 0 : s.GetHashCode())),
                    v => v.ToList()));
        }
    }
}
