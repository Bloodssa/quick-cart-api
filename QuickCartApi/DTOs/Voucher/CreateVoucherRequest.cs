using QuickCartApi.Enums;

namespace QuickCartApi.DTOs.Voucher
{
    public class CreateVoucherRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal Value { get; set; }
        public decimal MinSpend { get; set; }

        // percent vouchers only; null = no cap
        public decimal? MaxDiscount { get; set; }

        // null = unlimited
        public int? UsageLimit { get; set; }
        public int PerUserLimit { get; set; } = 1;

        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
    }

    public class SetVoucherActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
