using QuickCartApi.Enums;

namespace QuickCartApi.DTOs.Product
{
    public class ProductQuery
    {
        public int? CategoryId { get; set; }
        public string? Search { get; set; }
        public bool? Featured { get; set; }
        public bool? OnDeal { get; set; }       // only products with an active deal
        public bool? NewArrivals { get; set; }   // last 14 days
        public bool? BestSellers { get; set; }  // sold, or rated 3.8+
        public ProductSort? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
