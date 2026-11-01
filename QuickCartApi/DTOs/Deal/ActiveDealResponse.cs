namespace QuickCartApi.DTOs.Deal
{
    public class ActiveDealResponse
    {
        public int ProductId { get; set; }
        public int DealId { get; set; }
        public string DealName { get; set; } = string.Empty;
        public decimal RegularPrice { get; set; }
        public decimal DealPrice { get; set; }
        public int DiscountPercent { get; set; }
        public DateTime EndsAt { get; set; }
    }
}
