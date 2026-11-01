namespace QuickCartApi.DTOs.Reviews
{
    public class ReviewQuery
    {
        public int? Rating { get; set; }              // filter: only this star level
        public bool WithPhotos { get; set; }          // filter: only reviews with photos
        public string Sort { get; set; } = "newest";  // newest | highest | lowest
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
