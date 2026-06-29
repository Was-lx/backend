using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Infrastructure.Identity;

namespace WaslX.Persistance.Data.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.HasData(
            new ApplicationRole
            {
                Id = DefaultRoles.SuperAdminRoleId,
                Name = DefaultRoles.SuperAdmin,
                NormalizedName = DefaultRoles.SuperAdmin.ToUpper(),
                ConcurrencyStamp = DefaultRoles.SuperAdminRoleConcurrencyStamp,
            },
            new ApplicationRole
            {
                Id = DefaultRoles.AdminRoleId,
                Name = DefaultRoles.Admin,
                NormalizedName = DefaultRoles.Admin.ToUpper(),
                ConcurrencyStamp = DefaultRoles.AdminRoleConcurrencyStamp,
            },
            new ApplicationRole
            {
                Id = DefaultRoles.ManagerRoleId,
                Name = DefaultRoles.Manager,
                NormalizedName = DefaultRoles.Manager.ToUpper(),
                ConcurrencyStamp = DefaultRoles.ManagerRoleConcurrencyStamp,
            },
            new ApplicationRole
            {
                Id = DefaultRoles.AgentRoleId,
                Name = DefaultRoles.Agent,
                NormalizedName = DefaultRoles.Agent.ToUpper(),
                ConcurrencyStamp = DefaultRoles.AgentRoleConcurrencyStamp,
                IsDefault = true,
            });
    }
}
