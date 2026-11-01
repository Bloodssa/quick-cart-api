namespace QuickCartApi.DTOs.Admin.Customer
{
    public class CustomerOrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
