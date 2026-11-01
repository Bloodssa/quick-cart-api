namespace QuickCartApi.DTOs.Admin.Dashboard
{
    public class DashboardReviews
    {
        public int NeedsReply { get; set; }
        public int LowRatedNeedsReply { get; set; }
        public double Average30d { get; set; }
        public int Count30d { get; set; }
        public List<DashboardReview> Latest { get; set; } = [];
    }
}
