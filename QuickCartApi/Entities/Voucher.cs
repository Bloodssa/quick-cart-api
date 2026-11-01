using QuickCartApi.Enums;

namespace QuickCartApi.Entities
{
    public class Voucher : BaseEntity
    {
        public string Code { get; set; } = string.Empty;   // always stored UPPERCASE
        public string Title { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal Value { get; set; }                 // percent (1-90) or peso amount off
        public decimal MinSpend { get; set; }              // 0 = no minimum
        public decimal? MaxDiscount { get; set; }          // cap for percent vouchers
        public int? UsageLimit { get; set; }               // null = unlimited
        public int UsedCount { get; set; }
        public int PerUserLimit { get; set; } = 1;
        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public bool IsActive { get; set; } = true;

        public VoucherStatus StatusAt(DateTime nowUtc)
        {
            if (!IsActive) return VoucherStatus.Disabled;
            if (nowUtc < StartsAtUtc) return VoucherStatus.Scheduled;
            if (nowUtc >= EndsAtUtc) return VoucherStatus.Expired;
            if (UsageLimit is { } limit && UsedCount >= limit) return VoucherStatus.UsedUp;
            return VoucherStatus.Active;
        }
    }
}
