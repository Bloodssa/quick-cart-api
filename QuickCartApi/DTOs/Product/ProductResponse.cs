namespace QuickCartApi.DTOs.Product
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }

        // set only while a discount is active on this product
        public decimal? DealPrice { get; set; }
        public int? DiscountPercent { get; set; }
        public DateTime? DealEndsAt { get; set; }
        public int SoldCount { get; set; }

        public double AverageRating { get; set; }   // 0 when there are no reviews
        public int ReviewCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public List<ProductImageResponse> Images { get; set; } = [];
    }
}
