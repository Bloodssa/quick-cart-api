namespace QuickCartApi.DTOs.Deal
{
    public class DealProductResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal RegularPrice { get; set; }
        public decimal DealPrice { get; set; }
    }
}
