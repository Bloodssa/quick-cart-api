namespace QuickCartApi.DTOs.Admin.Customer
{
    public class CustomerListResponse
    {
        public List<CustomerSummaryResponse> Customers { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalResults { get; set; }
    }
}
