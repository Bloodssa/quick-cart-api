using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickCartApi.Entities;

namespace QuickCartApi.Data.Configurations
{
    public class ReviewImageConfiguration : IEntityTypeConfiguration<ReviewImage>
    {
        public void Configure(EntityTypeBuilder<ReviewImage> builder)
        {
            builder.Property(i => i.FileName).HasMaxLength(100).IsRequired();

            builder.HasOne(i => i.Review).WithMany(r => r.Images)
                .HasForeignKey(i => i.ReviewId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}