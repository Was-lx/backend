using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customers");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("customer_id");
            builder.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Tier).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Tenant).WithMany(x => x.Customers).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
