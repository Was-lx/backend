using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class PlatformCredentialConfiguration : IEntityTypeConfiguration<PlatformCredential>
    {
        public void Configure(EntityTypeBuilder<PlatformCredential> builder)
        {
            builder.ToTable("platform_credentials");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("platform_credential_id");
            builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
            builder.Property(x => x.EncryptedValue).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.Masked).HasMaxLength(100).IsRequired();

            builder.HasIndex(x => x.Key).IsUnique();
        }
    }
}
