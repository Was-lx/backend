using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AgentWhatsAppDistributionConfiguration : IEntityTypeConfiguration<AgentWhatsAppDistribution>
    {
        public void Configure(EntityTypeBuilder<AgentWhatsAppDistribution> builder)
        {
            builder.ToTable("agent_whatsapp_distribution");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("agent_whatsapp_distribution_id");
            builder.HasIndex(x => new { x.UserId, x.WhatsAppAccountId }).IsUnique();
            builder.HasOne(x => x.User).WithMany(x => x.AgentWhatsAppDistributions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.WhatsAppAccount).WithMany(x => x.AgentWhatsAppDistributions).HasForeignKey(x => x.WhatsAppAccountId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
