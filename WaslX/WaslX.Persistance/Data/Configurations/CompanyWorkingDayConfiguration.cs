using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class CompanyWorkingDayConfiguration : IEntityTypeConfiguration<CompanyWorkingDay>
    {
        public void Configure(EntityTypeBuilder<CompanyWorkingDay> builder)
        {
            builder.ToTable("company_working_days");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("company_working_day_id");
            builder.Property(x => x.DayOfWeek).HasConversion<int>();
            builder.HasIndex(x => new { x.TenantId, x.DayOfWeek }).IsUnique();
            builder.HasOne(x => x.Tenant).WithMany(x => x.CompanyWorkingDays).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
