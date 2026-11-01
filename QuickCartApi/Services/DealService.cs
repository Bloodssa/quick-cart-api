using QuickCartApi.Common;
using QuickCartApi.DTOs.Deal;
using QuickCartApi.Entities;
using QuickCartApi.Enums;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Services
{
    public class DealService(IDealRepository repository, TimeProvider clock) : IDealService
    {
        public async Task<ServiceResult<DealResponse>> CreateAsync(CreateDealRequest request)
        {
            var now = Now();
            var start = request.StartsAt.ToUniversalTime();
            var end = request.EndsAt.ToUniversalTime();
            var productIds = (request.ProductIds ?? []).Distinct().ToList();

            var error = DealRules.Validate(request.Name, request.DiscountType, request.Value, start, end, productIds.Count, now);
            if (error is not null)
                return Result.Fail<DealResponse>(ErrorKind.Validation, error);

            var products = await repository.GetProductsAsync(productIds);
            if (products.Count != productIds.Count)
                return Result.Fail<DealResponse>(ErrorKind.NotFound, "Some of the selected products no longer exist.");

            if (request.DiscountType == DiscountType.Amount)
            {
                var tooCheap = products.FirstOrDefault(p => request.Value >= p.Price);
                if (tooCheap is not null)
                    return Result.Fail<DealResponse>(ErrorKind.Validation,
                        $"The amount off must be less than the price of '{tooCheap.Name}'.");
            }

            // a product can only be in one discount at a time
            var clashes = await repository.GetClashingProductNamesAsync(productIds, start, end);
            if (clashes.Count > 0)
                return Result.Fail<DealResponse>(ErrorKind.Conflict,
                    $"Already in another discount during that time: {string.Join(", ", clashes)}.");

            var deal = new Deal
            {
                Name = request.Name.Trim(),
                DiscountType = request.DiscountType,
                Value = request.Value,
                StartsAtUtc = start,
                EndsAtUtc = end,
                Products = products.Select(p => new DealProduct { ProductId = p.Id, Product = p }).ToList()
            };

            await repository.AddAsync(deal);
            return Result.Ok(ToResponse(deal, now));
        }

        public async Task<List<DealResponse>> GetAllAsync(DealStatus? status)
        {
            var now = Now();
            var deals = await repository.GetRecentAsync();

            // status depends on the clock, so it is filtered here instead of in SQL
            return deals
                .Where(d => status is null || d.StatusAt(now) == status)
                .Select(d => ToResponse(d, now))
                .ToList();
        }

        public async Task<ServiceResult> EndAsync(int id)
        {
            var now = Now();

            var deal = await repository.GetByIdAsync(id);
            if (deal is null)
                return Result.Fail(ErrorKind.NotFound, "Discount not found.");

            var status = deal.StatusAt(now);
            if (status is not (DealStatus.Scheduled or DealStatus.Active))
                return Result.Fail(ErrorKind.Validation, $"A {status} discount can't be ended.");

            deal.CancelledAtUtc = now;
            await repository.UpdateAsync(deal);
            return Result.Ok();
        }

        public async Task<List<ActiveDealResponse>> GetActiveAsync()
        {
            var rows = await repository.GetActiveDealProductsAsync(null, Now());
            return PickBestPerProduct(rows).Values.ToList();
        }

        public async Task<Dictionary<int, ActiveDealResponse>> GetActivePricesAsync(List<int> productIds)
        {
            if (productIds.Count == 0) return new();

            var rows = await repository.GetActiveDealProductsAsync(productIds, Now());
            return PickBestPerProduct(rows);
        }

        /**
         * HELPERS
         */

        private DateTime Now() => clock.GetUtcNow().UtcDateTime;

        // EF returns dates as Unspecified; mark them UTC so the JSON ends with "Z"
        private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

        // overlaps are blocked on create, but if two ever exist the lower price wins
        private static Dictionary<int, ActiveDealResponse> PickBestPerProduct(List<DealProduct> rows) =>
            rows.Select(ToActive)
                .GroupBy(a => a.ProductId)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.DealPrice).First());

        private static ActiveDealResponse ToActive(DealProduct dp)
        {
            var regular = dp.Product.Price;
            var price = DealRules.PriceFor(dp.Deal.DiscountType, dp.Deal.Value, regular);

            return new ActiveDealResponse
            {
                ProductId = dp.ProductId,
                DealId = dp.DealId,
                DealName = dp.Deal.Name,
                RegularPrice = regular,
                DealPrice = price,
                DiscountPercent = DealRules.PercentOff(regular, price),
                EndsAt = Utc(dp.Deal.EndsAtUtc)
            };
        }

        private static DealResponse ToResponse(Deal d, DateTime now) => new()
        {
            Id = d.Id,
            Name = d.Name,
            DiscountType = d.DiscountType,
            Value = d.Value,
            StartsAt = Utc(d.StartsAtUtc),
            EndsAt = Utc(d.EndsAtUtc),
            Status = d.StatusAt(now).ToString(),
            Products = d.Products
                .Select(dp => new DealProductResponse
                {
                    ProductId = dp.ProductId,
                    Name = dp.Product.Name,
                    RegularPrice = dp.Product.Price,
                    DealPrice = DealRules.PriceFor(d.DiscountType, d.Value, dp.Product.Price)
                })
                .OrderBy(p => p.Name)
                .ToList()
        };
    }
}