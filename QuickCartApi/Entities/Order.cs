using QuickCartApi.Enums;

namespace QuickCartApi.Entities
{
    public class Order : BaseEntity
    {
        public int UserId { get; set; }
        public int AddressId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public int? VoucherId { get; set; }
        public string? VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }        // the real price for this address
        public decimal ShippingDiscount { get; set; }
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime OrderDate { get; set; }
        public User User { get; set; } = null!;
        public Address Address { get; set; } = null!;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public Transaction? Transaction { get; set; }
    }
}
