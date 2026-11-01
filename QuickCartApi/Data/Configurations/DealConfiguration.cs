using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickCartApi.Entities;

namespace QuickCartApi.Data
{
    public class DealConfiguration : IEntityTypeConfiguration<Deal>
    {
        public void Configure(EntityTypeBuilder<Deal> builder)
        {
            builder.Property(d => d.Name).HasMaxLength(80);
            builder.Property(d => d.Value).HasPrecision(10, 2);

            builder.HasMany(d => d.Products).WithOne(dp => dp.Deal)
                .HasForeignKey(dp => dp.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(d => new { d.StartsAtUtc, d.EndsAtUtc });
        }
    }
}