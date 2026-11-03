using Microsoft.EntityFrameworkCore;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Deal;
using QuickCartApi.DTOs.Product;
using QuickCartApi.Entities;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services.Interfaces;
using static QuestPDF.Helpers.Colors;

namespace QuickCartApi.Services
{
    public class ProductService(
            IProductRepository productRepository,
            IProductImageRepository productImageRepository,
            IFileStorageService storage,
            IHttpContextAccessor httpContextAccessor,
            IDealService dealService
        ) : IProductService
    {
        private const int LowStockThreshold = 10;

        public async Task<ProductListResponse> GetAllAsync(ProductQuery q)
        {
            q.Page = q.Page < 1 ? 1 : q.Page;
            q.PageSize = q.PageSize is < 1 or > 100 ? 20 : q.PageSize;

            var (items, totalCount) = await productRepository.GetAllAsync(q);
            var ids = items.Select(p => p.Id).ToList();

            var deals = await dealService.GetActivePricesAsync(ids);
            var sold = await productRepository.GetSoldCountsAsync(ids);
            var ratings = await productRepository.GetRatingStatsAsync(ids);

            return new ProductListResponse
            {
                Products = items.Select(p => ToResponse(
                    p,
                    deals.GetValueOrDefault(p.Id),
                    sold.GetValueOrDefault(p.Id),
                    ratings.GetValueOrDefault(p.Id))).ToList(),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalResults = totalCount
            };
        }

        public async Task<ProductResponse?> GetByIdAsync(int id)
        {
            var product = await productRepository.GetByIdAsync(id);
            if (product is null || product.ArchivedAtUtc is not null) return null;

            var deals = await dealService.GetActivePricesAsync([product.Id]);
            var sold = await productRepository.GetSoldCountsAsync([product.Id]);
            var ratings = await productRepository.GetRatingStatsAsync([product.Id]);

            return ToResponse(product, deals.GetValueOrDefault(product.Id), sold.GetValueOrDefault(product.Id), ratings.GetValueOrDefault(product.Id));
        }

        public async Task<(ProductResponse? Product, string? error)> CreateAsync(CreateProductRequest request)
        {
            if (!await productRepository.CategoryExistsAsync(request.CategoryId))
                return (null, "Category does not exists.");

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                IsFeatured = request.IsFeatured,
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await productRepository.AddAsync(product);
            await SaveImagesAsync(created.Id, request.Images, makeFirstDefault: true);

            var withImages = await productRepository.GetByIdAsync(created.Id);
            return (ToResponse(withImages!), null);
        }

        public async Task<(bool Success, string? Error, ProductResponse? response)> UpdateAsync(int id, UpdateProductRequest request)
        {
            var product = await productRepository.GetByIdAsync(id);
            if (product is null) return (false, "Product not found.", null);

            if (!await productRepository.CategoryExistsAsync(request.CategoryId))
                return (false, "Category does not exists.", null)!;

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.IsFeatured = request.IsFeatured;
            product.CategoryId = request.CategoryId;

            await productRepository.UpdateAsync(product);
            await SaveImagesAsync(id, request.Images, makeFirstDefault: !product.Images.Any(i => i.IsDefault));
            var updated = await productRepository.GetByIdAsync(id);

            return (true, null, ToResponse(updated!));
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var product = await productRepository.GetByIdAsync(id);
            if (product is null)
                return Result.Fail(ErrorKind.NotFound, "Product not found.");

            // nobody should keep it in a cart, whichever way it goes
            await productRepository.RemoveFromCartsAsync(id);

            // ordered or in a discount: archive it, so order history keeps its product
            if (await productRepository.IsUsedAsync(id))
            {
                await productRepository.ArchiveAsync(id);
                return Result.Ok();
            }

            try
            {
                await productRepository.DeleteAsync(product);
                return Result.Ok();
            }
            catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
            {
                // 547 = foreign key conflict from something not checked above (reviews, for example)
                await productRepository.ArchiveAsync(id);
                return Result.Ok();
            }
        }


        public async Task<(bool Success, string? Error, int Stock)> AdjustStockAsync(int id, int delta)
        {
            if (delta == 0) return (false, "Change must not be zero.", 0);

            var stock = await productRepository.AdjustStockAsync(id, delta);
            if (stock is not null) return (true, null, stock.Value);

            // Nothing updated: either the product doesn't exist or stock would go below zero
            return await productRepository.ExistsAsync(id)
                ? (false, "Stock cannot go below zero.", 0)
                : (false, "Product not found.", 0);
        }

        public async Task<(bool Success, string? Error)> DeleteImageAsync(int productId, int imageId)
        {
            var image = await productImageRepository.GetByIdAsync(imageId);
            if (image is null || image.ProductId != productId)
                return (false, "Image not found.");

            var fileName = Path.GetFileName(image.ImageUrl);
            storage.DeleteImage(fileName);

            await productImageRepository.DeleteAsync(image);
            return (true, null);
        }


        public Task<StockSummaryResponse> GetStockSummaryAsync()
            => productRepository.GetStockSummaryAsync(LowStockThreshold);

        public async Task<(bool Success, string? Error)> SetDefaultImageAsync(int productId, int imageId)
        {
            var image = await productImageRepository.GetByIdAsync(imageId);
            if (image is null || image.ProductId != productId)
                return (false, "Image not found.");

            await productImageRepository.ClearDefaultAsync(productId);
            image.IsDefault = true;
            await productImageRepository.UpdateAsync(image);

            return (true, null);
        }

        private ProductResponse ToResponse(Product product, ActiveDealResponse? deal = null, int soldCount = 0, (double Average, int Count) rating = default) => new ()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            IsFeatured = product.IsFeatured,
            SoldCount = soldCount,
            AverageRating = rating.Average,
            ReviewCount = rating.Count,
            DealPrice = deal?.DealPrice,
            DiscountPercent = deal?.DiscountPercent,
            DealEndsAt = deal?.EndsAt,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            CreatedAt = product.CreatedAt,
            Images = product.Images?.Select(i => new ProductImageResponse
            {
                Id = i.Id,
                ImageUrl = GetFullImageUrl(i.ImageUrl),
                IsDefault = i.IsDefault
            }).ToList() ?? []
        };

        private async Task SaveImagesAsync(int productId, IEnumerable<IFormFile>? files, bool makeFirstDefault)
        {
            if (files is null) return;

            var isFirst = true;
            foreach (var file in files)
            {
                var (success, fileName, _) = await storage.SaveImageAsync(file);
                if (!success) continue;

                await productImageRepository.AddAsync(new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = $"/uploads/products/{fileName}",
                    IsDefault = makeFirstDefault && isFirst
                });
                isFirst = false;
            }
        }

        private string GetFullImageUrl(string url)
        {
            var request = httpContextAccessor.HttpContext?.Request;

            if (request is null)
                return url;

            return $"{request.Scheme}://{request.Host}{url}";
        }
    }
}
