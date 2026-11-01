namespace QuickCartApi.Common
{
    public class ShippingOptions
    {
        public const string SectionName = "Shipping";

        public decimal FreeShippingThreshold { get; set; } = 8000m;
        public decimal MaxShippingFee { get; set; } = 200m;
        public Dictionary<string, decimal> Rates { get; set; } = new();   // key = ShippingZone name
        public List<ShippingTierOption> Tiers { get; set; } = [];
    }

    public class ShippingTierOption
    {
        public decimal Share { get; set; }   // share of the threshold, e.g. 0.40 = 40%
        public int Percent { get; set; }     // % off shipping
    }
}
