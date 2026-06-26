using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("invoices");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("invoice_id");
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Tenant).WithMany(x => x.Invoices).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}