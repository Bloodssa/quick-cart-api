using QuickCartApi.Enums;

namespace QuickCartApi.DTOs.Deal
{
    public class DealResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal Value { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<DealProductResponse> Products { get; set; } = [];
    }
}
