namespace QuickCartApi.DTOs.Reviews
{
    public class ReviewSummaryResponse
    {
        public double Average { get; set; }
        public int Total { get; set; }
        public int WithPhotos { get; set; }
        public List<RatingBucket> Distribution { get; set; } = []; // 5 stars down to 1
    }
}
