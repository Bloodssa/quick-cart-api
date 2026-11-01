namespace QuickCartApi.Entities
{
    public class ProductReview : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int Rating { get; set; }          // 1 to 5
        public string? Comment { get; set; }
        public string? AdminReply { get; set; }
        public DateTime? RepliedAtUtc { get; set; }
        public DateTime? ReplyEditedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public List<ReviewImage> Images { get; set; } = [];
    }
}
