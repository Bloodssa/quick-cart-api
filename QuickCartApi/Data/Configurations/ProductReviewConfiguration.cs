using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickCartApi.Entities;

namespace QuickCartApi.Data.Configurations
{
    public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
    {
        public void Configure(EntityTypeBuilder<ProductReview> builder)
        {
            builder.Property(r => r.Comment).HasMaxLength(2000);

            // one review per customer per product (also guards against double-submit races)
            builder.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
            builder.HasIndex(r => new { r.ProductId, r.CreatedAtUtc });

            builder.ToTable(t => t.HasCheckConstraint("CK_ProductReviews_Rating", "[Rating] BETWEEN 1 AND 5"));

            builder.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(r => r.AdminReply).HasMaxLength(1000);
        }
    }
}
