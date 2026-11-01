using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using QuickCartApi.Enums;

namespace QuickCartApi.Common;

    public record NextShippingDiscount(decimal Remaining, int Percent);

    public record ShippingQuote(
        string Zone,
        string ZoneLabel,
        decimal Fee,                   // the real shipping price
        int DiscountPercent,
        decimal Discount,              // taken off because the order is big enough
        decimal Payable,               // what the customer pays: Fee - Discount
        decimal FreeShippingThreshold,
        NextShippingDiscount? Next
     );

    // the shop is in Mandaue City, Cebu
    public class ShippingCalculator(IOptions<ShippingOptions> options)
    {
        private readonly ShippingOptions _options = options.Value;

        private static readonly Dictionary<ShippingZone, string> Labels = new()
        {
            [ShippingZone.Mandaue] = "Mandaue City",
            [ShippingZone.MetroCebu] = "Metro Cebu",
            [ShippingZone.CebuProvince] = "Rest of Cebu",
            [ShippingZone.Visayas] = "Visayas",
            [ShippingZone.Mindanao] = "Mindanao",
            [ShippingZone.Luzon] = "Luzon",
        };

        private static readonly HashSet<string> MetroCebuCities = new(StringComparer.OrdinalIgnoreCase)
        {
            "cebu", "lapu-lapu", "talisay", "consolacion", "liloan", "cordova", "minglanilla", "compostela", "naga"
        };

        private static readonly HashSet<string> VisayasProvinces = new(StringComparer.OrdinalIgnoreCase)
        {
            "bohol", "leyte", "southern leyte", "biliran", "samar", "western samar", "eastern samar", "northern samar",
            "negros oriental", "negros occidental", "siquijor", "iloilo", "antique", "capiz", "aklan", "guimaras"
        };

        private static readonly HashSet<string> MindanaoProvinces = new(StringComparer.OrdinalIgnoreCase)
        {
            "davao del sur", "davao del norte", "davao de oro", "davao oriental", "davao occidental",
            "zamboanga del norte", "zamboanga del sur", "zamboanga sibugay", "misamis oriental", "misamis occidental",
            "bukidnon", "camiguin", "lanao del norte", "lanao del sur", "agusan del norte", "agusan del sur",
            "surigao del norte", "surigao del sur", "dinagat islands", "cotabato", "south cotabato", "sultan kudarat",
            "sarangani", "maguindanao", "maguindanao del norte", "maguindanao del sur", "basilan", "sulu", "tawi-tawi"
        };

        public ShippingQuote Quote(string? province, string? city, decimal subtotal)
        {
            subtotal = Math.Max(0m, subtotal);
            var threshold = _options.FreeShippingThreshold;

            var zone = ZoneFor(province, city);

            // a zone missing from the settings is charged the cap, never more
            var rate = _options.Rates.TryGetValue(zone.ToString(), out var configured) ? configured : _options.MaxShippingFee;
            var fee = Math.Min(rate, _options.MaxShippingFee);

            var tiers = _options.Tiers.OrderBy(t => t.Share).ToList();

            var percent = 0;
            foreach (var tier in tiers)
                if (subtotal >= tier.Share * threshold) percent = tier.Percent;

            var discount = Math.Round(fee * percent / 100m, 2, MidpointRounding.AwayFromZero);

            var upcoming = tiers.FirstOrDefault(t => subtotal < t.Share * threshold);
            NextShippingDiscount? next = upcoming is null
                ? null
                : new NextShippingDiscount(Math.Round(upcoming.Share * threshold - subtotal, 2), upcoming.Percent);

            return new ShippingQuote(zone.ToString(), Labels[zone], fee, percent, discount, fee - discount, threshold, next);
        }

        public static ShippingZone ZoneFor(string? province, string? city)
        {
            var p = Normalize(province);
            var c = Normalize(city);

            if (c == "mandaue") return ShippingZone.Mandaue;
            if (p == "cebu") return MetroCebuCities.Contains(c) ? ShippingZone.MetroCebu : ShippingZone.CebuProvince;
            if (VisayasProvinces.Contains(p)) return ShippingZone.Visayas;
            if (MindanaoProvinces.Contains(p)) return ShippingZone.Mindanao;
            return ShippingZone.Luzon; // everything else, including Metro Manila and an unknown province
        }

        // "City of Lapu-Lapu (Opon)", "Lapu-Lapu City" -> "lapu-lapu"
        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var t = Regex.Replace(value.ToLowerInvariant(), @"\(.*?\)", "");
            t = t.Replace("province of", "").Replace("province", "").Replace("city of", "").Replace(" city", "");
            return Regex.Replace(t, @"\s+", " ").Trim();
        }
    }