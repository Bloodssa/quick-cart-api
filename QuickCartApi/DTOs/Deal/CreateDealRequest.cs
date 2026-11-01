using QuickCartApi.Enums;

namespace QuickCartApi.DTOs.Deal
{
    public class CreateDealRequest
    {
        public string Name { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal Value { get; set; }

        // UTC ISO strings, e.g. "2026-11-01T10:00:00.000Z"
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public List<int> ProductIds { get; set; } = [];
    }
}
