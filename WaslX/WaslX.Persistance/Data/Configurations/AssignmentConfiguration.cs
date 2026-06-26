using WaslX.Domain.Entities;
using WaslX.Domain.SharedEnums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
    {
        public void Configure(EntityTypeBuilder<Assignment> builder)
        {
            builder.ToTable("assignments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("assignment_id");
            builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            builder.HasOne(x => x.Conversation).WithMany(x => x.Assignments).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.AssignedToUser).WithMany(x => x.Assignments).HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}