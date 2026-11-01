using QuickCartApi.Enums;

namespace QuickCartApi.Services
{
    // Pure rules, free of EF, so they are easy to unit test.
    public static class DealRules
    {
        public const int MaxDays = 90;
        public const int MaxProducts = 50;

        public static decimal PriceFor(DiscountType type, decimal value, decimal regularPrice)
        {
            var price = type == DiscountType.Percent
                ? Math.Round(regularPrice * (100m - value) / 100m, 2, MidpointRounding.AwayFromZero)
                : regularPrice - value;

            // never free, never above the regular price (covers a price that changed after the deal was made)
            return Math.Min(Math.Max(price, 0.01m), regularPrice);
        }

        public static int PercentOff(decimal regularPrice, decimal dealPrice) =>
            regularPrice <= 0
                ? 0
                : (int)Math.Round((1 - dealPrice / regularPrice) * 100m, MidpointRounding.AwayFromZero);

        public static string? Validate(
            string? name, DiscountType type, decimal value,
            DateTime start, DateTime end, int productCount, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
                return "Give the discount a name of up to 80 characters.";
            if (type == DiscountType.Percent && (value < 1 || value > 90))
                return "A percent discount must be between 1% and 90%.";
            if (type == DiscountType.Amount && value <= 0)
                return "The amount off must be more than 0.";
            if (start < now.AddMinutes(-1))
                return "The discount can't start in the past.";
            if (end <= start)
                return "The end time must be after the start time.";
            if (end - start > TimeSpan.FromDays(MaxDays))
                return $"A discount can run for at most {MaxDays} days.";
            if (productCount < 1)
                return "Choose at least one product.";
            if (productCount > MaxProducts)
                return $"A discount can include at most {MaxProducts} products.";

            return null;
        }
    }
}