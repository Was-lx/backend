using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{
    public class InternalNoteConfiguration : IEntityTypeConfiguration<InternalNote>
    {
        public void Configure(EntityTypeBuilder<InternalNote> builder)
        {
            builder.ToTable("internal_notes");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("note_id");
            builder.Property(x => x.Content).IsRequired();
            builder.HasOne(x => x.Conversation).WithMany(x => x.InternalNotes).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.User).WithMany(x => x.InternalNotes).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}