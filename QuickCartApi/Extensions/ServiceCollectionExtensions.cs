using Microsoft.AspNetCore.Identity;
using QuickCartApi.Common;
using QuickCartApi.Entities;
using QuickCartApi.Repositories;
using QuickCartApi.Repositories.Interfaces;
using QuickCartApi.Services;
using QuickCartApi.Services.Interfaces;

namespace QuickCartApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddSingleton(TimeProvider.System);
            services.AddHttpContextAccessor();
                
            // Repositories
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductImageRepository, ProductImageRepository>();
            services.AddScoped<ICartItemRepository, CartItemRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IDealRepository, DealRepository>();
            services.AddScoped<IVoucherRepository, VoucherRepository>();

            // Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddSingleton<ShippingCalculator>();
            services.AddScoped<IDealService, DealService>();
            services.AddScoped<IVoucherService, VoucherService>();

            return services;
        }
    }
}