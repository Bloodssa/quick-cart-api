using Microsoft.EntityFrameworkCore;
using QuickCartApi.Data;
using QuickCartApi.Entities;
using QuickCartApi.Enums;
using QuickCartApi.Repositories.Interfaces;

namespace QuickCartApi.Repositories;

public class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public Task<bool> ProductExistsAsync(int productId) =>
        context.Products.AnyAsync(p => p.Id == productId);

    // verified purchase: a COMPLETED order of this customer that contains the product
    public Task<bool> HasPurchasedAsync(int userId, int productId) =>
        context.Orders.AnyAsync(o =>
            o.UserId == userId &&
            o.Status == OrderStatus.Completed &&
            o.Items.Any(i => i.ProductId == productId));

    // tracked, so adding/removing images saves together with the review
    public Task<ProductReview?> GetByUserAsync(int productId, int userId) =>
        context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

    public Task<ProductReview?> GetByIdAsync(int id) =>
        context.ProductReviews.AsNoTracking()
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<Dictionary<int, int>> GetRatingCountsAsync(int productId) =>
        context.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == productId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Rating, x => x.Count);

    public Task<int> CountWithPhotosAsync(int productId) =>
        context.ProductReviews.CountAsync(r => r.ProductId == productId && r.Images.Any());

    public async Task<(List<ProductReview> Items, int Total)> GetPageAsync(
        int productId, int? rating, bool withPhotos, string sort, int page, int pageSize)
    {
        var query = context.ProductReviews.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Images)
            .Where(r => r.ProductId == productId);

        if (rating is not null)
            query = query.Where(r => r.Rating == rating);

        if (withPhotos)
            query = query.Where(r => r.Images.Any());

        var total = await query.CountAsync();

        var ordered = sort switch
        {
            "highest" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAtUtc),
            "lowest" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAtUtc),
            _ => query.OrderByDescending(r => r.CreatedAtUtc)
        };

        var items = await ordered
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> AddAsync(ProductReview review)
    {
        context.ProductReviews.Add(review);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // the unique (ProductId, UserId) index: the customer double-submitted
            context.Entry(review).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(ProductReview review) => await context.SaveChangesAsync();

    public async Task DeleteAsync(ProductReview review)
    {
        context.ProductReviews.Remove(review);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteByIdAsync(int id) =>
        await context.ProductReviews.Where(r => r.Id == id).ExecuteDeleteAsync() > 0;

    public async Task<(List<ProductReview> Items, int Total)> GetAdminPageAsync(
    string filter, bool withPhotos, string? search, int page, int pageSize)
    {
        IQueryable<ProductReview> query = context.ProductReviews.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.Images);

        query = filter switch
        {
            "needsReply" => query.Where(r => r.AdminReply == null),
            "low" => query.Where(r => r.Rating <= 2),
            _ => query
        };

        if (withPhotos)
            query = query.Where(r => r.Images.Any());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.Product.Name.Contains(term) || r.User.Username.Contains(term));
        }

        var total = await query.CountAsync();

        // unanswered: the unhappiest customers first; everything else: newest first
        var ordered = filter == "needsReply"
            ? query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAtUtc)
            : query.OrderByDescending(r => r.CreatedAtUtc);

        var items = await ordered
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    // counts for the tabs; not affected by the search or the current tab
    public async Task<(int All, int NeedsReply, int Low)> GetAdminCountsAsync()
    {
        var row = await context.ProductReviews.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                All = g.Count(),
                NeedsReply = g.Count(r => r.AdminReply == null),
                Low = g.Count(r => r.Rating <= 2)
            })
            .FirstOrDefaultAsync();

        return row is null ? (0, 0, 0) : (row.All, row.NeedsReply, row.Low);
    }

    // tracked, so a reply change saves with UpdateAsync
    public Task<ProductReview?> GetForAdminAsync(int id) =>
        context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id);
}