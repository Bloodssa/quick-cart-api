using QuickCartApi.DTOs.Product;
using QuickCartApi.Entities;

namespace QuickCartApi.Repositories.Interfaces
{
    public interface IProductRepository : IBaseRepository<Product>
    {
        Task<(List<Product> Items, int TotalCount)> GetAllAsync(ProductQuery pq);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task<bool> TryDecrementStockAsync(int productId, int quantity);
        Task IncrementStockAsync(int productId, int quantity);
        Task<int?> AdjustStockAsync(int id, int delta); // new stock, or null if nothing was updated
        Task<bool> ExistsAsync(int id);
        Task<StockSummaryResponse> GetStockSummaryAsync(int lowStockThreshold);
        Task<bool> IsUsedAsync(int productId);
        Task ArchiveAsync(int productId);
        Task RemoveFromCartsAsync(int productId);
        Task<Dictionary<int, int>> GetSoldCountsAsync(List<int> productIds);
        Task<Dictionary<int, (double Average, int Count)>> GetRatingStatsAsync(List<int> productIds);
    }
}
