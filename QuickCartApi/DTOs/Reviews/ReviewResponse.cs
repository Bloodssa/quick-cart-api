namespace QuickCartApi.DTOs.Reviews
{
    public class ReviewResponse
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public bool ReplyEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsMine { get; set; }
        public List<ReviewImageResponse> Images { get; set; } = [];
    }
}
