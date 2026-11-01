using QuickCartApi.DTOs.Address;

namespace QuickCartApi.DTOs.Order
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? VoucherCode { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal ShippingDiscount { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public AddressResponse Address { get; set; } = null!;
        public List<OrderItemResponse> Items { get; set; } = [];
        public PaymentResponse? Payment { get; set; }
    }
}
