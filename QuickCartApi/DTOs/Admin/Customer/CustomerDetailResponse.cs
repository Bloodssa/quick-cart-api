namespace QuickCartApi.DTOs.Admin.Customer
{
    public class CustomerDetailResponse : CustomerSummaryResponse
    {
        public List<CustomerOrderResponse> RecentOrders { get; set; } = [];
    }
}
