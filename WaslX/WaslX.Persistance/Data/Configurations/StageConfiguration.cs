using WaslX.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WaslX.Persistance.Configurations
{

    public class StageConfiguration : IEntityTypeConfiguration<Stage>
    {
        public void Configure(EntityTypeBuilder<Stage> builder)
        {
            builder.ToTable("stages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("stage_id");
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.HasOne(x => x.Group).WithMany(x => x.Stages).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}