namespace QuickCartApi.Entities
{
    public class ReviewImage
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public ProductReview Review { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
    }
}
