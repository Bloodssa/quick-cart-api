namespace QuickCartApi.DTOs.Reviews
{
    public class ReviewListResponse
    {
        public List<ReviewResponse> Reviews { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalResults { get; set; }
    }
}
