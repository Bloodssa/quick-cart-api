using System.Text.RegularExpressions;
using QuickCartApi.Entities;
using QuickCartApi.Enums;

namespace QuickCartApi.Services
{
    // Pure rules, free of EF, so they are easy to unit test.
    public static class VoucherRules
    {
        private static readonly Regex CodePattern = new("^[A-Z0-9]{4,20}$");

        public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

        // the discount a voucher gives on a cart: capped by MaxDiscount, never more than the cart itself
        public static decimal Discount(Voucher v, decimal subTotal)
        {
            var raw = v.DiscountType == DiscountType.Percent
                ? Math.Round(subTotal * v.Value / 100m, 2, MidpointRounding.AwayFromZero)
                : v.Value;

            if (v.MaxDiscount is { } cap) raw = Math.Min(raw, cap);
            return Math.Min(raw, subTotal);
        }

        public static string? ValidateNew(
            string code, string? title, DiscountType type, decimal value, decimal minSpend,
            decimal? maxDiscount, int? usageLimit, int perUserLimit,
            DateTime start, DateTime end, DateTime now)
        {
            if (!CodePattern.IsMatch(code))
                return "Use 4 to 20 letters or numbers for the code.";
            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 80)
                return "Give the voucher a title of up to 80 characters.";
            if (type == DiscountType.Percent && (value < 1 || value > 90))
                return "A percent voucher must be between 1% and 90%.";
            if (type == DiscountType.Amount && value <= 0)
                return "The amount off must be more than 0.";
            if (minSpend < 0)
                return "The minimum spend can't be negative.";
            if (type == DiscountType.Amount && minSpend > 0 && value >= minSpend)
                return "The amount off must be less than the minimum spend.";
            if (maxDiscount is not null && (type != DiscountType.Percent || maxDiscount <= 0))
                return "Max discount is for percent vouchers only, and must be more than 0.";
            if (usageLimit is < 1)
                return "The usage limit must be at least 1.";
            if (perUserLimit < 1)
                return "Uses per customer must be at least 1.";
            if (start < now.AddMinutes(-1))
                return "The voucher can't start in the past.";
            if (end <= start)
                return "The end time must be after the start time.";

            return null;
        }
    }
}