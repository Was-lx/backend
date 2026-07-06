using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> builder)
        {
            builder.ToTable("payment_methods");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("payment_method_id");
            builder.Property(x => x.Brand).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Last4).HasMaxLength(4).IsRequired();
            builder.Property(x => x.HolderName).HasMaxLength(120);
            builder.HasOne(x => x.Tenant).WithMany(x => x.PaymentMethods).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
