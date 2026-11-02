namespace QuickCartApi.DTOs.Order.Customer
{
    public class OrderPreviewItem
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public decimal RegularPrice { get; set; }
        public decimal UnitPrice { get; set; }      // the deal price when a deal is active
        public int DiscountPercent { get; set; }    // 0 when there is no deal
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
    }
}
