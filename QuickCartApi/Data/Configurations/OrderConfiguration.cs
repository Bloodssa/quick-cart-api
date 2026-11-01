using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickCartApi.Entities;

namespace QuickCartApi.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
            builder.Property(o => o.Status).HasMaxLength(20).IsRequired();
            builder.Property(o => o.SubTotal).HasPrecision(10, 2);
            builder.Property(o => o.Total).HasPrecision(10, 2);

            builder.HasIndex(o => o.OrderNumber).IsUnique();

            builder.HasOne(o => o.User)
                .WithMany(o => o.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // address -> orders
            builder.HasOne(o => o.Address)
                .WithMany()
                .HasForeignKey(o => o.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(o => o.ShippingFee).HasPrecision(18, 2);
            builder.Property(o => o.ShippingDiscount).HasPrecision(18, 2);
            builder.Property(o => o.DiscountAmount).HasPrecision(10, 2);
            builder.Property(o => o.VoucherCode).HasMaxLength(20);
        }
    }
}
