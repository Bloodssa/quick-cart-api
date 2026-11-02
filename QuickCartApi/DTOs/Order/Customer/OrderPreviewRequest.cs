namespace QuickCartApi.DTOs.Order.Customer
{
    public class OrderPreviewRequest
    {
        public int AddressId { get; set; }
        public List<int> CartItemIds { get; set; } = [];

        public string? VoucherCode { get; set; }
    }
}
