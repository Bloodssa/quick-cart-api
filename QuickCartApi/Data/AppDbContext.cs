using Microsoft.EntityFrameworkCore;
using QuickCartApi.Entities;

namespace QuickCartApi.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
        public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();
        public DbSet<Deal> Deals => Set<Deal>();
        public DbSet<DealProduct> DealProducts => Set<DealProduct>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
