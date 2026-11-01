namespace QuickCartApi.DTOs.Reviews.Admin
{
    public class AdminReviewQuery
    {
        public string Filter { get; set; } = "needsReply";   // needsReply | low | all
        public bool WithPhotos { get; set; }
        public string? Search { get; set; }                   // product name or customer name
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
