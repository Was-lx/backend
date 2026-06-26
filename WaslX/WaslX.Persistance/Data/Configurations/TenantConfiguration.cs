using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("tenants");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("tenant_id");
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.BillingStatus).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Plan).WithMany(x => x.Tenants).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.PlatformUser).WithMany(x => x.Tenants).HasForeignKey(x => x.PlatformUserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
