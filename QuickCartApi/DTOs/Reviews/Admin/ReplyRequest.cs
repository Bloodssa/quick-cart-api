using System.ComponentModel.DataAnnotations;

namespace QuickCartApi.DTOs.Reviews.Admin
{
    public class ReplyRequest
    {
        [Required(ErrorMessage = "Write a reply.")]
        [MaxLength(1000, ErrorMessage = "Reply must be 1000 characters or fewer.")]
        public string Reply { get; set; } = string.Empty;
    }
}
