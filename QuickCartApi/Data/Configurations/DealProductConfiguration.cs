using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickCartApi.Entities;

namespace QuickCartApi.Data
{
    public class DealProductConfiguration : IEntityTypeConfiguration<DealProduct>
    {
        public void Configure(EntityTypeBuilder<DealProduct> builder)
        {
            builder.HasKey(dp => new { dp.DealId, dp.ProductId });

            builder.HasOne(dp => dp.Product).WithMany()
                .HasForeignKey(dp => dp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(dp => dp.ProductId);
        }
    }
}