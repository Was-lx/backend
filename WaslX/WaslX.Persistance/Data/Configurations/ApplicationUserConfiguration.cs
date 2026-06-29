using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Infrastructure.Identity;

namespace WaslX.Persistance.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(100);

        builder.OwnsMany(u => u.RefreshTokens)
            .ToTable("RefreshTokens")
            .WithOwner()
            .HasForeignKey("UserId");

        var passwordHasher = new PasswordHasher<ApplicationUser>();

        builder.HasData(new ApplicationUser
        {
            Id = DefaultUsers.SuperAdminId,
            Email = DefaultUsers.SuperAdminEmail,
            NormalizedEmail = DefaultUsers.SuperAdminEmail.ToUpper(),
            UserName = DefaultUsers.SuperAdminUserName,
            NormalizedUserName = DefaultUsers.SuperAdminUserName.ToUpper(),
            FullName = DefaultUsers.SuperAdminFullName,
            ConcurrencyStamp = DefaultUsers.SuperAdminConcurrencyStamp,
            SecurityStamp = DefaultUsers.SuperAdminSecurityStamp,
            EmailConfirmed = true,
            TenantId = null,
            PasswordHash = passwordHasher.HashPassword(null!, DefaultUsers.SuperAdminPassword),
        });
    }
}
