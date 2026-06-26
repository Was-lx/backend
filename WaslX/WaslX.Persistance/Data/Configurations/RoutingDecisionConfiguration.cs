using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class RoutingDecisionConfiguration : IEntityTypeConfiguration<RoutingDecision>
    {
        public void Configure(EntityTypeBuilder<RoutingDecision> builder)
        {
            builder.ToTable("routing_decisions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("decision_id");
            builder.Property(x => x.Topic).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Language).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Sentiment).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(50);
            builder.HasOne(x => x.Conversation).WithMany(x => x.RoutingDecisions).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.RecommendedUser).WithMany(x => x.RoutingDecisions).HasForeignKey(x => x.RecommendedUserId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}