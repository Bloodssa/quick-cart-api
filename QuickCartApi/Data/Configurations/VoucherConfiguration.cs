using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickCartApi.Entities;

namespace QuickCartApi.Data
{
    public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
    {
        public void Configure(EntityTypeBuilder<Voucher> builder)
        {
            builder.Property(v => v.Code).HasMaxLength(20);
            builder.HasIndex(v => v.Code).IsUnique();

            builder.Property(v => v.Title).HasMaxLength(80);
            builder.Property(v => v.Value).HasPrecision(10, 2);
            builder.Property(v => v.MinSpend).HasPrecision(10, 2);
            builder.Property(v => v.MaxDiscount).HasPrecision(10, 2);

            // orders remember which voucher they used (so a cancelled order can give the use back)
            builder.HasMany<Order>().WithOne().HasForeignKey(o => o.VoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            // last line of defence against over-claiming a voucher
            builder.ToTable(t => t.HasCheckConstraint("CK_Vouchers_UsedCount", "[UsedCount] >= 0 AND ([UsageLimit] IS NULL OR [UsedCount] <= [UsageLimit])"));
        }
    }
}