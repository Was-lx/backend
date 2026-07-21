using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaslX.Domain.Entities;

namespace WaslX.Persistance.Data.Configurations
{
    public class AiAgentNumberSettingsConfiguration : IEntityTypeConfiguration<AiAgentNumberSettings>
    {
        public void Configure(EntityTypeBuilder<AiAgentNumberSettings> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasOne(s => s.WhatsAppAccount)
                .WithMany()
                .HasForeignKey(s => s.WhatsAppAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.UpdatedByUser)
                .WithMany()
                .HasForeignKey(s => s.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(s => s.WhatsAppAccountId)
                .IsUnique();
        }
    }
}
