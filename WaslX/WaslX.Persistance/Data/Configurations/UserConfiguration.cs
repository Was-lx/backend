using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("user_id");
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            builder.HasOne(x => x.Tenant).WithMany(x => x.Users).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}