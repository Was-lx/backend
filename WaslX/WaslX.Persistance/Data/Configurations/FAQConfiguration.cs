using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class FAQConfiguration : IEntityTypeConfiguration<FAQ>
    {
        public void Configure(EntityTypeBuilder<FAQ> builder)
        {
            builder.ToTable("faqs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("faq_id");
            builder.Property(x => x.Question).IsRequired();
            builder.Property(x => x.Answer).IsRequired();
            builder.Property(x => x.Language).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Tenant).WithMany(x => x.FAQs).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}