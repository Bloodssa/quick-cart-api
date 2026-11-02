using QuickCartApi.Common;

namespace QuickCartApi.DTOs.Order.Customer
{
    public class OrderPreviewResponse
    {
        public List<OrderPreviewItem> Items { get; set; } = [];

        // after product deals, before the voucher
        public decimal SubTotal { get; set; }

        // regular prices minus deal prices
        public decimal ProductSavings { get; set; }

        // set only when the typed code was accepted
        public string? VoucherCode { get; set; }
        public decimal VoucherDiscount { get; set; }

        // why the typed code was refused (the rest of the preview still works)
        public string? VoucherError { get; set; }

        public ShippingQuote Shipping { get; set; } = null!;
        public decimal Total { get; set; }
    }
}
