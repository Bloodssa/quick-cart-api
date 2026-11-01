namespace QuickCartApi.DTOs.Reviews.Admin
{
    public class AdminReviewListResponse
    {
        public List<AdminReviewResponse> Reviews { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalResults { get; set; }
        public AdminReviewCounts Counts { get; set; } = new();
    }
}
