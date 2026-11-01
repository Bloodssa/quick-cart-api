using QuickCartApi.Enums;

namespace QuickCartApi.Entities
{
    public class Deal : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal Value { get; set; }

        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }

        public List<DealProduct> Products { get; set; } = [];

        public DealStatus StatusAt(DateTime nowUtc)
        {
            if (CancelledAtUtc is not null) return DealStatus.Cancelled;
            if (nowUtc < StartsAtUtc) return DealStatus.Scheduled;
            if (nowUtc >= EndsAtUtc) return DealStatus.Ended;
            return DealStatus.Active;
        }
    }
}
