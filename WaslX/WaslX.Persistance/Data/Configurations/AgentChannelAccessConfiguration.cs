using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AgentChannelAccessConfiguration : IEntityTypeConfiguration<AgentChannelAccess>
    {
        public void Configure(EntityTypeBuilder<AgentChannelAccess> builder)
        {
            builder.ToTable("agent_channel_access");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("agent_channel_access_id");
            builder.HasIndex(x => new { x.UserId, x.ChannelId }).IsUnique();
            builder.HasOne(x => x.User).WithMany(x => x.AgentChannelAccesses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Channel).WithMany(x => x.AgentChannelAccesses).HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
