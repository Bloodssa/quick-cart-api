namespace QuickCartApi.DTOs.Reviews.Admin
{
    public class AdminReviewResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string CustomerName { get; set; } = string.Empty;   // not masked: the admin sees who wrote it
        public DateTime CreatedAt { get; set; }
        public List<ReviewImageResponse> Images { get; set; } = [];
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public bool ReplyEdited { get; set; }
    }
}
