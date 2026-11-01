using System.ComponentModel.DataAnnotations;

namespace QuickCartApi.DTOs.Reviews
{
    public class SaveReviewRequest
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [MaxLength(2000, ErrorMessage = "Review must be 2000 characters or fewer.")]
        public string? Comment { get; set; }
        public List<IFormFile> Images { get; set; } = []; // new photos
        public List<int> RemoveImageIds { get; set; } = []; // existing photos to delete (edit only)
    }
}
