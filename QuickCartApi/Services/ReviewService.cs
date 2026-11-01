using QuickCartApi.Common;
using QuickCartApi.DTOs.Reviews;
using QuickCartApi.DTOs.Reviews.Admin;
using QuickCartApi.Entities;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Services;

    public class ReviewService(
        IReviewRepository repository,
        IFileStorageService storage,
        IHttpContextAccessor http,
        TimeProvider clock
    ) : IReviewService
    {
        private const int MaxPageSize = 50;
        private const int MaxImages = 5;
        private const string ImageFolder = "reviews";

        public async Task<ServiceResult<ReviewSummaryResponse>> GetSummaryAsync(int productId)
        {
            if (!await repository.ProductExistsAsync(productId))
                return Result.Fail<ReviewSummaryResponse>(ErrorKind.NotFound, "Product not found.");

            var counts = await repository.GetRatingCountsAsync(productId);
            var total = counts.Values.Sum();
            var average = total == 0 ? 0 : Math.Round(counts.Sum(c => c.Key * c.Value) / (double)total, 1);

            return Result.Ok(new ReviewSummaryResponse
            {
                Average = average,
                Total = total,
                WithPhotos = await repository.CountWithPhotosAsync(productId),
                Distribution = new[] { 5, 4, 3, 2, 1 }
                    .Select(s => new RatingBucket { Stars = s, Count = counts.GetValueOrDefault(s) })
                    .ToList()
            });
        }

        public async Task<ServiceResult<ReviewListResponse>> GetReviewsAsync(int productId, ReviewQuery query, int? currentUserId)
        {
            if (query.Rating is < 1 or > 5)
                return Result.Fail<ReviewListResponse>(ErrorKind.Validation, "Rating filter must be between 1 and 5.");

            if (!await repository.ProductExistsAsync(productId))
                return Result.Fail<ReviewListResponse>(ErrorKind.NotFound, "Product not found.");

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            var sort = (query.Sort ?? "newest").ToLowerInvariant();

            var (items, total) = await repository.GetPageAsync(productId, query.Rating, query.WithPhotos, sort, page, pageSize);

            var baseUrl = BaseUrl;
            return Result.Ok(new ReviewListResponse
            {
                Reviews = items.Select(r => ToResponse(r, currentUserId, baseUrl)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalResults = total
            });
        }

        public async Task<ServiceResult<MyReviewResponse>> GetMineAsync(int productId, int userId)
        {
            if (!await repository.ProductExistsAsync(productId))
                return Result.Fail<MyReviewResponse>(ErrorKind.NotFound, "Product not found.");

            var review = await repository.GetByUserAsync(productId, userId);
            if (review is not null)
                return Result.Ok(new MyReviewResponse { Purchased = true, Review = ToResponse(review, userId, BaseUrl) });

            var purchased = await repository.HasPurchasedAsync(userId, productId);
            return Result.Ok(new MyReviewResponse { Purchased = purchased });
        }

        public async Task<ServiceResult<bool>> CreateAsync(int productId, int userId, SaveReviewRequest request)
        {
            if (!await repository.ProductExistsAsync(productId))
                return Result.Fail<bool>(ErrorKind.NotFound, "Product not found.");

            if (!await repository.HasPurchasedAsync(userId, productId))
                return Result.Fail<bool>(ErrorKind.Validation,
                    "Only customers who have received this product can review it.");

            if (await repository.GetByUserAsync(productId, userId) is not null)
                return Result.Fail<bool>(ErrorKind.Conflict, "You have already reviewed this product.");

            if (request.Images.Count > MaxImages)
                return Result.Fail<bool>(ErrorKind.Validation, $"You can attach up to {MaxImages} photos.");

            var (files, uploadError) = await SaveFilesAsync(request.Images);
            if (uploadError is not null)
                return Result.Fail<bool>(ErrorKind.Validation, uploadError);

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = request.Rating,
                Comment = Clean(request.Comment),
                CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
                Images = files.Select(f => new ReviewImage { FileName = f }).ToList()
            };

            if (!await repository.AddAsync(review))
            {
                DeleteFiles(files);
                return Result.Fail<bool>(ErrorKind.Conflict, "You have already reviewed this product.");
            }

            return Result.Ok(true);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(int productId, int userId, SaveReviewRequest request)
        {
            var review = await repository.GetByUserAsync(productId, userId);
            if (review is null)
                return Result.Fail<bool>(ErrorKind.NotFound, "You haven't reviewed this product yet.");

            var removeIds = request.RemoveImageIds.ToHashSet();
            var removed = review.Images.Where(i => removeIds.Contains(i.Id)).ToList(); // only this review's own images

            if (review.Images.Count - removed.Count + request.Images.Count > MaxImages)
                return Result.Fail<bool>(ErrorKind.Validation, $"You can attach up to {MaxImages} photos.");

            var (files, uploadError) = await SaveFilesAsync(request.Images);
            if (uploadError is not null)
                return Result.Fail<bool>(ErrorKind.Validation, uploadError);

            foreach (var image in removed) review.Images.Remove(image);
            review.Images.AddRange(files.Select(f => new ReviewImage { FileName = f }));
            review.Rating = request.Rating;
            review.Comment = Clean(request.Comment);
            review.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;

            await repository.UpdateAsync(review);
            DeleteFiles(removed.Select(i => i.FileName)); // only after the DB save succeeded

            return Result.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteMineAsync(int productId, int userId)
        {
            var review = await repository.GetByUserAsync(productId, userId);
            if (review is null)
                return Result.Fail<bool>(ErrorKind.NotFound, "Review not found.");

            var names = review.Images.Select(i => i.FileName).ToList();
            await repository.DeleteAsync(review);
            DeleteFiles(names);

            return Result.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int reviewId)
        {
            var review = await repository.GetByIdAsync(reviewId);
            if (review is null)
                return Result.Fail<bool>(ErrorKind.NotFound, "Review not found.");

            var names = review.Images.Select(i => i.FileName).ToList();
            await repository.DeleteByIdAsync(reviewId);
            DeleteFiles(names);

            return Result.Ok(true);
        }

        public async Task<AdminReviewListResponse> GetAdminReviewsAsync(AdminReviewQuery query)
        {
            var filter = query.Filter is "needsReply" or "low" ? query.Filter : "all";
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            var (items, total) = await repository.GetAdminPageAsync(filter, query.WithPhotos, query.Search, page, pageSize);
            var (all, needsReply, low) = await repository.GetAdminCountsAsync();

            var baseUrl = BaseUrl;
            return new AdminReviewListResponse
            {
                Reviews = items.Select(r => ToAdminResponse(r, baseUrl)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalResults = total,
                Counts = new AdminReviewCounts { All = all, NeedsReply = needsReply, Low = low }
            };
        }

        // one reply per review: the first call posts it, later calls edit it
        public async Task<ServiceResult<AdminReviewResponse>> SetReplyAsync(int reviewId, string reply)
        {
            var text = reply?.Trim();
            if (string.IsNullOrEmpty(text))
                return Result.Fail<AdminReviewResponse>(ErrorKind.Validation, "Write a reply.");

            var review = await repository.GetForAdminAsync(reviewId);
            if (review is null)
                return Result.Fail<AdminReviewResponse>(ErrorKind.NotFound, "Review not found.");

            var now = clock.GetUtcNow().UtcDateTime;

            if (review.AdminReply is null)
            {
                review.RepliedAtUtc = now;
                review.ReplyEditedAtUtc = null;
            }
            else if (review.AdminReply != text)
            {
                review.ReplyEditedAtUtc = now;
            }

            review.AdminReply = text;
            await repository.UpdateAsync(review);

            return Result.Ok(ToAdminResponse(review, BaseUrl));
        }

        public async Task<ServiceResult<bool>> DeleteReplyAsync(int reviewId)
        {
            var review = await repository.GetForAdminAsync(reviewId);
            if (review is null)
                return Result.Fail<bool>(ErrorKind.NotFound, "Review not found.");

            review.AdminReply = null;
            review.RepliedAtUtc = null;
            review.ReplyEditedAtUtc = null;
            await repository.UpdateAsync(review);

            return Result.Ok(true);
        }

    // ---- helpers ----

        private async Task<(List<string> Files, string? Error)> SaveFilesAsync(List<IFormFile> files)
            {
                var saved = new List<string>();
                foreach (var file in files)
                {
                    var (ok, name, error) = await storage.SaveImageAsync(file, ImageFolder);
                    if (!ok)
                    {
                        DeleteFiles(saved); // don't leave half an upload behind
                        return ([], error);
                    }
                    saved.Add(name!);
                }
                return (saved, null);
            }

        private void DeleteFiles(IEnumerable<string> fileNames)
        {
            foreach (var name in fileNames)
            {
                try { storage.DeleteImage(name, ImageFolder); }
                catch { /* an orphaned file is harmless */ }
            }
        }

        private string BaseUrl
        {
            get
            {
                var req = http.HttpContext!.Request;
                return $"{req.Scheme}://{req.Host}";
            }
        }

        private static string? Clean(string? comment) =>
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        // EF returns dates as Unspecified; mark them UTC so the JSON ends with "Z"
        private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

        private static string Mask(string? name)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name)) return "Customer";
            return name;
        }

        private static ReviewResponse ToResponse(ProductReview r, int? currentUserId, string baseUrl) => new()
        {
            Id = r.Id,
            Rating = r.Rating,
            Comment = r.Comment,
            ReviewerName = Mask(r.User.Username),
            CreatedAt = Utc(r.CreatedAtUtc),
            UpdatedAt = r.UpdatedAtUtc is null ? null : Utc(r.UpdatedAtUtc.Value),
            IsMine = r.UserId == currentUserId,
            Images = ToImages(r, baseUrl),
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAtUtc is null ? null : Utc(r.RepliedAtUtc.Value),
            ReplyEdited = r.ReplyEditedAtUtc is not null
        };

        private static List<ReviewImageResponse> ToImages(ProductReview r, string baseUrl) =>
            r.Images
                .Select(i => new ReviewImageResponse { Id = i.Id, ImageUrl = $"{baseUrl}/uploads/{ImageFolder}/{i.FileName}" })
                .ToList();

        private static AdminReviewResponse ToAdminResponse(ProductReview r, string baseUrl) => new()
        {
            Id = r.Id,
            ProductId = r.ProductId,
            ProductName = r.Product.Name,
            Rating = r.Rating,
            Comment = r.Comment,
            CustomerName = r.User.Username,
            CreatedAt = Utc(r.CreatedAtUtc),
            Images = ToImages(r, baseUrl),
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAtUtc is null ? null : Utc(r.RepliedAtUtc.Value),
            ReplyEdited = r.ReplyEditedAtUtc is not null
        };
}