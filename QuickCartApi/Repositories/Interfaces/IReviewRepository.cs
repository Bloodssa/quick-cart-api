using QuickCartApi.Entities;

namespace QuickCartApi.Repositories.Interfaces;

public interface IReviewRepository
{
    Task<bool> ProductExistsAsync(int productId);
    Task<bool> HasPurchasedAsync(int userId, int productId);
    Task<ProductReview?> GetByUserAsync(int productId, int userId);
    Task<ProductReview?> GetByIdAsync(int id);
    Task<Dictionary<int, int>> GetRatingCountsAsync(int productId);
    Task<int> CountWithPhotosAsync(int productId);
    Task<(List<ProductReview> Items, int Total)> GetPageAsync(int productId, int? rating, bool withPhotos, string sort, int page, int pageSize);
    Task<bool> AddAsync(ProductReview review);
    Task UpdateAsync(ProductReview review);
    Task DeleteAsync(ProductReview review);
    Task<bool> DeleteByIdAsync(int id);
    Task<(List<ProductReview> Items, int Total)> GetAdminPageAsync(string filter, bool withPhotos, string? search, int page, int pageSize);
    Task<(int All, int NeedsReply, int Low)> GetAdminCountsAsync();
    Task<ProductReview?> GetForAdminAsync(int id);
}