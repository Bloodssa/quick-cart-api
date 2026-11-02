using QuickCartApi.Common;
using QuickCartApi.DTOs.Product;

namespace QuickCartApi.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductListResponse> GetAllAsync(ProductQuery q);
        Task<ProductResponse?> GetByIdAsync(int id);
        Task<(ProductResponse? Product, string? error)> CreateAsync(CreateProductRequest request);
        Task<(bool Success, string? Error, ProductResponse? response)> UpdateAsync(int id, UpdateProductRequest request);
        Task<ServiceResult> DeleteAsync(int id);

        Task<(bool Success, string? Error)> DeleteImageAsync(int productId, int imageId);
        Task<(bool Success, string? Error)> SetDefaultImageAsync(int productId, int imageId);
        Task<(bool Success, string? Error, int Stock)> AdjustStockAsync(int id, int delta);
        Task<StockSummaryResponse> GetStockSummaryAsync();
    }
}
