using Microsoft.EntityFrameworkCore;
using QuickCartApi.Data;
using QuickCartApi.DTOs.Product;
using QuickCartApi.Entities;
using QuickCartApi.Enums;
using QuickCartApi.Repositories.Interfaces;

namespace QuickCartApi.Repositories
{
    public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
    {
        private const int TrendingDays = 30;
        private const int NewArrivalDays = 14;
        private const double BestSellerMinRating = 3.8;

        public async Task<(List<Product> Items, int TotalCount)> GetAllAsync(ProductQuery q)
        {
            var now = DateTime.UtcNow;

            IQueryable<Product> query = context.Products.Where(p => p.ArchivedAtUtc == null);

            if (q.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == q.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(q.Search))
                query = query.Where(p => p.Name.Contains(q.Search));

            if (q.Featured.HasValue)
                query = query.Where(p => p.IsFeatured == q.Featured.Value);

            // deals: only products in a deal that is not cancelled and is running right now
            if (q.OnDeal == true)
            {
                query = query.Where(p => context.DealProducts.Any(dp =>
                    dp.ProductId == p.Id
                    && dp.Deal.CancelledAtUtc == null
                    && dp.Deal.StartsAtUtc <= now
                    && dp.Deal.EndsAtUtc > now));
            }

            // new arrivals: added within the last 2 weeks
            if (q.NewArrivals == true)
            {
                var since = now.AddDays(-NewArrivalDays);
                query = query.Where(p => p.CreatedAt >= since);
            }

            // best sellers: has completed sales, or an average rating of 3.8 or more
            if (q.BestSellers == true)
            {
                query = query.Where(p =>
                    p.OrderItems.Any(oi => oi.Order.Status == OrderStatus.Completed) ||
                    (p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= BestSellerMinRating));
            }

            var totalCount = await query.CountAsync();

            var items = await ApplySort(query, q.Sort ?? ProductSort.Newest)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsNoTracking()
                .AsSplitQuery()
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public override async Task<Product?> GetByIdAsync(int id)
            => await context.Products
                   .Include(p => p.Category)
                   .Include(p => p.Images)
                   .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<bool> CategoryExistsAsync(int categoryId) =>
            await context.Categories.AnyAsync(c => c.Id == categoryId);

        public async Task<bool> TryDecrementStockAsync(int productId, int quantity)
        {
            var rows = await context.Products
                .Where(p => p.Id == productId && p.StockQuantity >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity));

            return rows > 0;
        }

        public async Task<int?> AdjustStockAsync(int id, int delta)
        {
            // One atomic UPDATE: the WHERE clause refuses to push stock below zero
            var rows = await context.Products
                .Where(p => p.Id == id && p.StockQuantity + delta >= 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + delta));

            if (rows == 0) return null;

            return await context.Products
                .Where(p => p.Id == id)
                .Select(p => p.StockQuantity)
                .SingleAsync();
        }

        public async Task<StockSummaryResponse> GetStockSummaryAsync(int lowStockThreshold)
        {
            var summary = await context.Products
                .GroupBy(_ => 1)
                .Select(g => new StockSummaryResponse
                {
                    TotalUnits = g.Sum(p => p.StockQuantity),
                    LowCount = g.Count(p => p.StockQuantity > 0 && p.StockQuantity <= lowStockThreshold),
                    OutCount = g.Count(p => p.StockQuantity <= 0)
                })
                .FirstOrDefaultAsync();

            // No products yet: GroupBy returns no rows
            summary ??= new StockSummaryResponse();
            summary.LowStockThreshold = lowStockThreshold;
            return summary;
        }

        public Task<bool> ExistsAsync(int id) => context.Products.AnyAsync(p => p.Id == id);

        public async Task IncrementStockAsync(int productId, int quantity) =>
            await context.Products
                .Where(p => p.Id == productId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity));

        public async Task<bool> IsUsedAsync(int productId) =>
            await context.OrderItems.AnyAsync(oi => oi.ProductId == productId) || await context.DealProducts.AnyAsync(dp => dp.ProductId == productId);

        public async Task ArchiveAsync(int productId) =>
            await context.Products
                .Where(p => p.Id == productId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ArchivedAtUtc, DateTime.UtcNow));

        public async Task RemoveFromCartsAsync(int productId) =>
            await context.CartItems.Where(ci => ci.ProductId == productId).ExecuteDeleteAsync();

        public async Task<Dictionary<int, int>> GetSoldCountsAsync(List<int> productIds)
        {
            if (productIds.Count == 0) return new();

            return await context.OrderItems
                .Where(i => productIds.Contains(i.ProductId) && i.Order.Status == OrderStatus.Completed)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Sold = g.Sum(i => i.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Sold);
        }

        private static IOrderedQueryable<Product> ApplySort(IQueryable<Product> query, ProductSort sort)
        {
            var since = DateTime.UtcNow.AddDays(-TrendingDays);

            return sort switch
            {
                ProductSort.PriceAsc => query.OrderBy(p => p.Price).ThenByDescending(p => p.Id),
                ProductSort.PriceDesc => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.Id),
                ProductSort.Popular => query
                    .OrderByDescending(p => p.OrderItems
                        .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
                        .Sum(oi => oi.Quantity)
                    ).ThenByDescending(p => p.CreatedAt)
                    .ThenByDescending(p => p.Id),

                ProductSort.Trending => query
                    .OrderByDescending(p => p.OrderItems
                    .Where(oi => oi.Order.Status != OrderStatus.Cancelled && oi.Order.OrderDate >= since)
                    .Sum(oi => oi.Quantity))
                    .ThenByDescending(p => p.CreatedAt) 
                    .ThenByDescending(p => p.Id),

                _ => query                                   // Newest
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenByDescending(p => p.Id)
            };
        }
    }
}
