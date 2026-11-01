namespace QuickCartApi.DTOs.Admin.Customer
{
    public class CustomerSummaryResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; } // paid orders only
    }
}
