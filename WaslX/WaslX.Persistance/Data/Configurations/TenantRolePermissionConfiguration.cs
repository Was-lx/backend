using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class TenantRolePermissionConfiguration : IEntityTypeConfiguration<TenantRolePermission>
    {
        public void Configure(EntityTypeBuilder<TenantRolePermission> builder)
        {
            builder.ToTable("tenant_role_permissions");
            builder.HasKey(x => new { x.TenantId, x.Role, x.PermissionId });
            builder.Property(x => x.Role).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ScopeValue).HasMaxLength(50);
            builder.HasOne(x => x.Tenant).WithMany(x => x.RolePermissions).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Permission).WithMany(x => x.TenantGrants).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.TenantId, x.Role });
        }
    }
}
