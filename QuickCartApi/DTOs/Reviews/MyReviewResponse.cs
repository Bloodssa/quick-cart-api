namespace QuickCartApi.DTOs.Reviews
{
    public class MyReviewResponse
    {
        public bool Purchased { get; set; }
        public ReviewResponse? Review { get; set; }
    }
}
