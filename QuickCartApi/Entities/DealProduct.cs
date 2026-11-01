namespace QuickCartApi.Entities
{
    public class DealProduct
    {
        public int DealId { get; set; }
        public int ProductId { get; set; }
        public Deal Deal { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
