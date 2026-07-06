using System;
using System.Linq;
using WaslX.Domain.Authorization;
using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        // Fixed timestamp keeps the HasData seed deterministic across migrations.
        private static readonly DateTime SeedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("permissions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("permission_id");
            builder.Property(x => x.Code).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Tier).HasConversion<string>().HasMaxLength(30);
            builder.Property(x => x.ScopeOptions).HasMaxLength(200);
            builder.HasIndex(x => x.Code).IsUnique();

            // Seed the whole permission catalog (system-level definitions).
            builder.HasData(PermissionCatalog.All.Select(p => new Permission
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                Category = p.Category,
                Tier = p.Tier,
                IsScope = p.IsScope,
                ScopeOptions = p.ScopeOptions,
                SortOrder = p.Sort,
                CreatedAt = SeedDate,
            }).ToList());
        }
    }
}
