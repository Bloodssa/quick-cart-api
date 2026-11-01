using QuickCartApi.Enums;

namespace QuickCartApi.DTOs.Voucher
{
    public class VoucherResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal Value { get; set; }
        public decimal MinSpend { get; set; }
        public decimal? MaxDiscount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int PerUserLimit { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
