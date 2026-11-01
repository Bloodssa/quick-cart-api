using Microsoft.EntityFrameworkCore;
using QuickCartApi.Data;
using QuickCartApi.Entities;
using QuickCartApi.Repositories.Interfaces;

namespace QuickCartApi.Repositories
{
    public class DealRepository(AppDbContext context) : IDealRepository
    {
        public Task<List<Product>> GetProductsAsync(List<int> productIds) =>
            context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        public Task<List<string>> GetClashingProductNamesAsync(List<int> productIds, DateTime startUtc, DateTime endUtc) =>
            context.DealProducts
                .Where(dp => productIds.Contains(dp.ProductId)
                             && dp.Deal.CancelledAtUtc == null
                             && dp.Deal.StartsAtUtc < endUtc
                             && dp.Deal.EndsAtUtc > startUtc)
                .Select(dp => dp.Product.Name)
                .Distinct()
                .ToListAsync();

        public async Task AddAsync(Deal deal)
        {
            context.Deals.Add(deal);
            await context.SaveChangesAsync();
        }

        public Task<Deal?> GetByIdAsync(int id) =>
            context.Deals.FirstOrDefaultAsync(d => d.Id == id);

        public async Task UpdateAsync(Deal deal) => await context.SaveChangesAsync();

        public Task<List<Deal>> GetRecentAsync() =>
            context.Deals.AsNoTracking()
                .Include(d => d.Products).ThenInclude(dp => dp.Product)
                .OrderByDescending(d => d.StartsAtUtc)
                .Take(200)
                .ToListAsync();

        public Task<List<DealProduct>> GetActiveDealProductsAsync(List<int>? productIds, DateTime nowUtc)
        {
            IQueryable<DealProduct> query = context.DealProducts.AsNoTracking()
                .Include(dp => dp.Deal)
                .Include(dp => dp.Product)
                .Where(dp => dp.Deal.CancelledAtUtc == null
                             && dp.Deal.StartsAtUtc <= nowUtc
                             && dp.Deal.EndsAtUtc > nowUtc);

            if (productIds is not null)
                query = query.Where(dp => productIds.Contains(dp.ProductId));

            return query.ToListAsync();
        }
    }
}