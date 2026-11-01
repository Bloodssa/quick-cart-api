using QuickCartApi.Entities;

namespace QuickCartApi.Repositories.Interfaces
{
    public interface IDealRepository
    {
        Task<List<Product>> GetProductsAsync(List<int> productIds);
        Task<List<string>> GetClashingProductNamesAsync(List<int> productIds, DateTime startUtc, DateTime endUtc);

        Task AddAsync(Deal deal);
        Task<Deal?> GetByIdAsync(int id);
        Task UpdateAsync(Deal deal); // the deal is tracked: this just saves the changes
        Task<List<Deal>> GetRecentAsync();

        // productIds = null returns every active deal product
        Task<List<DealProduct>> GetActiveDealProductsAsync(List<int>? productIds, DateTime nowUtc);
    }
}