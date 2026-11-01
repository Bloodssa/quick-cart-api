using Microsoft.EntityFrameworkCore;
using QuickCartApi.Common;
using QuickCartApi.Data;
using QuickCartApi.DTOs.Admin.Customer;
using QuickCartApi.DTOs.Admin.Dashboard;
using QuickCartApi.DTOs.Admin.Reports;
using QuickCartApi.Entities;
using QuickCartApi.Enums;
using QuickCartApi.Repositories.Interfaces;

namespace QuickCartApi.Repositories
{
    public class AdminRepository(AppDbContext context) : IAdminRepository
    {
        private const int LowStock = 10; // if product quantity is bbelow 10 mark it as lowstock
        private const int RangeDays = 30;
        private static readonly TimeSpan PhOffset = TimeSpan.FromHours(8); // ph time utc+8
        private IQueryable<Order> Orders = context.Orders.AsNoTracking(); // no track entities just read
        private IQueryable<Order> PaidOrders => Orders.Where(o => o.Transaction != null && o.Transaction.PaymentStatus == PaymentStatus.Paid); // paid orders
        private IQueryable<User> Customers => context.Users.AsNoTracking().Where(u => u.Role != "Admin");

        public async Task<DashboardResponse> GetDashboardAsync()
        {
            var nowUtc = DateTime.UtcNow;
            var todayStartUtc = (nowUtc + PhOffset).Date - PhOffset;
            var rangeStartUtc = todayStartUtc.AddDays(-(RangeDays - 1));

            var counts = await GetOrderCountsAsync(nowUtc, todayStartUtc);
            var (revenue, previousRevenue, paidOrderCount) = await GetRevenueTotalsAsync(rangeStartUtc);
            var stock = await GetStockCountsAsync();
            var customers = await GetCustomerCountsAsync(nowUtc);

            return new DashboardResponse
            {
                PendingOrders = counts.Pending,
                StaleOrders = counts.Stale,
                OrdersToday = counts.Today,
                OrdersYesterday = counts.Yesterday,
                Revenue30d = revenue,
                RevenueChangePct = previousRevenue > 0 ? Math.Round((revenue - previousRevenue) / previousRevenue * 100, 1) : null,
                AverageOrderValue = paidOrderCount > 0 ? Math.Round(revenue / paidOrderCount, 2) : 0,

                LowStockCount = stock.Low,
                OutOfStockCount = stock.Out,

                NewCustomers7d = customers.New,
                TotalCustomers = customers.Total,
                Reviews = await GetReviewStatsAsync(rangeStartUtc),

                OrdersByStatus = await GetOrdersByStatusAsync(),
                Restock = await GetRestockAsync(),
                RevenueByDay = await GetRevenueByDayAsync(rangeStartUtc, nowUtc),
                TopProducts = await GetTopProductsAsync(rangeStartUtc),
                NeedsAction = await GetNeedsActionAsync()
            };
        }

        private IQueryable<Order> InRange(ReportRange r) =>
        context.Orders.AsNoTracking().Where(o => o.OrderDate >= r.FromUtc && o.OrderDate < r.ToUtcExclusive);

        // Same "paid" rule as the dashboard: change it in one place if your revenue definition changes
        private static IQueryable<Order> Paid(IQueryable<Order> q) =>
            q.Where(o => o.Transaction != null && o.Transaction.PaymentStatus == PaymentStatus.Paid);

        public async Task<SalesReportSummary> GetSummaryAsync(ReportRange range)
        {
            var all = InRange(range);

            var totalOrders = await all.CountAsync();
            var cancelled = await all.CountAsync(o => o.Status == OrderStatus.Cancelled);

            var paid = await Paid(all)
                .GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Revenue = g.Sum(o => o.Total) })
                .FirstOrDefaultAsync(); // null when nothing was paid in the range

            var paidCount = paid?.Count ?? 0;
            var revenue = paid?.Revenue ?? 0m;

            return new SalesReportSummary
            {
                TotalOrders = totalOrders,
                CancelledOrders = cancelled,
                PaidOrders = paidCount,
                PaidRevenue = revenue,
                AverageOrderValue = paidCount > 0 ? Math.Round(revenue / paidCount, 2) : 0
            };
        }

        public async Task<List<RevenueDay>> GetRevenueByDayAsync(ReportRange range)
        {
            // Group by the Manila calendar day, not the UTC day
            var rows = await Paid(InRange(range))
                .GroupBy(o => o.OrderDate.AddHours(8).Date)
                .Select(g => new { Day = g.Key, Revenue = g.Sum(o => o.Total) })
                .ToListAsync();

            var byDay = rows.ToDictionary(r => DateOnly.FromDateTime(r.Day), r => r.Revenue);

            // Fill days with no sales so the chart has one bar per day
            return Enumerable.Range(0, range.Days)
                .Select(i => range.From.AddDays(i))
                .Select(d => new RevenueDay { Date = d, Revenue = byDay.GetValueOrDefault(d) })
                .ToList();
        }

        public Task<List<TopProduct>> GetTopProductsAsync(ReportRange range, int take) =>
            context.OrderItems
                .AsNoTracking()
                .Where(i => i.Order.OrderDate >= range.FromUtc
                         && i.Order.OrderDate < range.ToUtcExclusive
                         && i.Order.Status != OrderStatus.Cancelled)
                .GroupBy(i => new { i.ProductId, i.Product.Name })
                .Select(g => new TopProduct { ProductId = g.Key.ProductId, Name = g.Key.Name, Quantity = g.Sum(i => i.Quantity) })
                .OrderByDescending(p => p.Quantity)
                .Take(take)
                .ToListAsync();

        public async Task<(List<SalesTransactionRow> Rows, int Total)> GetTransactionsAsync(ReportRange range, int skip, int take)
        {
            var query = InRange(range).OrderByDescending(o => o.OrderDate);

            var total = await query.CountAsync();

            var rows = await query
                .Skip(skip)
                .Take(take)
                .Select(o => new SalesTransactionRow
                {
                    Date = o.OrderDate,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User.Username,
                    PaymentMethod = o.Transaction != null ? o.Transaction.PaymentMethod.ToString() : "",
                    PaymentStatus = o.Transaction != null ? o.Transaction.PaymentStatus.ToString() : "",
                    Total = o.Total
                })
                .ToListAsync();

            // SQL returns no timezone marker; mark UTC so the JSON ends with "Z"
            foreach (var r in rows) r.Date = DateTime.SpecifyKind(r.Date, DateTimeKind.Utc);

            return (rows, total);
        }

        public async Task<(List<CustomerSummaryResponse> Items, int Total)> GetCustomersAsync(string? search, int page, int pageSize)
        {
            var query = Customers;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u => u.Username.Contains(term) || u.Email.Contains(term));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new CustomerSummaryResponse
                {
                    Id = u.Id,
                    FullName = u.Username,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    OrderCount = context.Orders.Count(o => o.UserId == u.Id),
                    TotalSpent = context.Orders
                        .Where(o => o.UserId == u.Id && o.Transaction != null && o.Transaction.PaymentStatus == PaymentStatus.Paid)
                        .Sum(o => (decimal?)o.Total) ?? 0m
                })
                .ToListAsync();

            foreach (var c in items) c.CreatedAt = DateTime.SpecifyKind(c.CreatedAt, DateTimeKind.Utc);

            return (items, total);
        }

        public async Task<CustomerDetailResponse?> GetCustomerDetailAsync(int id)
        {
            var customer = await Customers
                .Where(u => u.Id == id)
                .Select(u => new CustomerDetailResponse
                {
                    Id = u.Id,
                    FullName = u.Username,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    OrderCount = context.Orders.Count(o => o.UserId == u.Id),
                    TotalSpent = context.Orders
                        .Where(o => o.UserId == u.Id && o.Transaction != null && o.Transaction.PaymentStatus == PaymentStatus.Paid)
                        .Sum(o => (decimal?)o.Total) ?? 0m
                })
                .FirstOrDefaultAsync();

            if (customer is null) return null;

            customer.CreatedAt = DateTime.SpecifyKind(customer.CreatedAt, DateTimeKind.Utc);

            customer.RecentOrders = await Orders
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new CustomerOrderResponse
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status.ToString(),
                    Total = o.Total,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            foreach (var o in customer.RecentOrders) o.OrderDate = DateTime.SpecifyKind(o.OrderDate, DateTimeKind.Utc);

            return customer;
        }

        // HELPERS FUNCTIONS

        // return tuple by counting the orders statuses
        private async Task<(int Pending, int Stale, int Today, int Yesterday)> GetOrderCountsAsync(DateTime nowUtc, DateTime todayStartUtc)
        {
            var yesterdayStartUtc = todayStartUtc.AddDays(-1);
            var tomorrowStartUtc = todayStartUtc.AddDays(1);
            var staleBefore = nowUtc.AddHours(-24);

            var row = await Orders
                        .GroupBy(_ => 1)
                        .Select(g => new
                        {
                            Pending = g.Count(o => o.Status == OrderStatus.Pending),
                            Stale = g.Count(o => o.Status == OrderStatus.Pending && o.OrderDate <= staleBefore),
                            Today = g.Count(o => o.OrderDate >= todayStartUtc && o.OrderDate < tomorrowStartUtc),
                            Yesterday = g.Count(o => o.OrderDate >= yesterdayStartUtc && o.OrderDate < todayStartUtc)
                        })
                        .FirstOrDefaultAsync(); // null when there are no orders at all

            return row is null ? (0, 0, 0, 0) : (row.Pending, row.Stale, row.Today, row.Yesterday);
        }

        // current 30 days and the 30 days before, in one query
        private async Task<(decimal Current, decimal Previous, int CurrentCount)> GetRevenueTotalsAsync(DateTime rangeStartUtc)
        {
            var previousStartUtc = rangeStartUtc.AddDays(-RangeDays);

            var row = await PaidOrders
                .Where(o => o.OrderDate >= previousStartUtc)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Current = g.Where(o => o.OrderDate >= rangeStartUtc).Sum(o => (decimal?)o.Total) ?? 0m, // sum the toatl of the orders from based on the 30 days total
                    Previous = g.Where(o => o.OrderDate < rangeStartUtc).Sum(o => (decimal?)o.Total) ?? 0m,
                    CurrentCount = g.Count(o => o.OrderDate >= rangeStartUtc)
                })
                .FirstOrDefaultAsync();

            return row is null ? (0, 0, 0) : (row.Current, row.Previous, row.CurrentCount);
        }

        // unanswered reviews, the 30-day rating, and the 5 that need attention first (lowest stars, then newest)
        private async Task<DashboardReviews> GetReviewStatsAsync(DateTime rangeStartUtc)
        {
            var reviews = context.ProductReviews.AsNoTracking();

            var pending = await reviews
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    NeedsReply = g.Count(r => r.AdminReply == null),
                    Low = g.Count(r => r.AdminReply == null && r.Rating <= 2)
                })
                .FirstOrDefaultAsync();

            var recent = await reviews
                .Where(r => r.CreatedAtUtc >= rangeStartUtc)
                .GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Average = g.Average(r => (double)r.Rating) })
                .FirstOrDefaultAsync();

            var latest = await reviews
                .Where(r => r.AdminReply == null)
                .OrderBy(r => r.Rating)
                .ThenByDescending(r => r.CreatedAtUtc)
                .Take(5)
                .Select(r => new DashboardReview
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,
                    CustomerName = r.User.Username,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAtUtc
                })
                .ToListAsync();

            foreach (var r in latest)
            {
                r.CreatedAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc);
                if (r.Comment is { Length: > 140 }) r.Comment = r.Comment[..140] + "…";
            }

            return new DashboardReviews
            {
                NeedsReply = pending?.NeedsReply ?? 0,
                LowRatedNeedsReply = pending?.Low ?? 0,
                Count30d = recent?.Count ?? 0,
                Average30d = recent is null ? 0 : Math.Round(recent.Average, 1),
                Latest = latest
            };
        }

        private async Task<(int Low, int Out)> GetStockCountsAsync()
        {
            var row = await context.Products
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Low = g.Count(p => p.StockQuantity > 0 && p.StockQuantity <= LowStock),
                    Out = g.Count(p => p.StockQuantity <= 0)
                })
                .FirstOrDefaultAsync();

            return row is null ? (0, 0) : (row.Low, row.Out);
        }

        private async Task<(int Total, int New)> GetCustomerCountsAsync(DateTime nowUtc)
        {
            var since = nowUtc.AddDays(-7);

            var row = await context.Users
                .AsNoTracking()
                .Where(u => u.Role != "Admin")
                .GroupBy(_ => 1)
                .Select(g => new { Total = g.Count(), New = g.Count(u => u.CreatedAt >= since) })
                .FirstOrDefaultAsync();

            return row is null ? (0, 0) : (row.Total, row.New);
        }

        private async Task<Dictionary<OrderStatus, int>> GetOrdersByStatusAsync()
        {
            var counts = await Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // always return every status, even with 0 so no gap in chart
            return Enum.GetValues<OrderStatus>()
                .ToDictionary(s => s, s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);
        }

        private Task<List<RestockItem>> GetRestockAsync() =>
            context.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity <= LowStock)
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.Name)
                .Take(5)
                .Select(p => new RestockItem { Id = p.Id, Name = p.Name, Stock = p.StockQuantity })
                .ToListAsync();

        private async Task<List<RevenueDay>> GetRevenueByDayAsync(DateTime rangeStartUtc, DateTime nowUtc)
        {
            // group by the Manila calendar day
            var rows = await PaidOrders
                .Where(o => o.OrderDate >= rangeStartUtc)
                .GroupBy(o => o.OrderDate.AddHours(8).Date)
                .Select(g => new { Day = g.Key, Revenue = g.Sum(o => o.Total) })
                .ToListAsync();

            var byDay = rows.ToDictionary(r => DateOnly.FromDateTime(r.Day), r => r.Revenue);
            var today = DateOnly.FromDateTime(nowUtc + PhOffset);

            // fill days that had no sales with 0 so the chart has all 30 bars
            return Enumerable.Range(0, RangeDays)
                .Select(i => today.AddDays(-(RangeDays - 1 - i)))
                .Select(d => new RevenueDay { Date = d, Revenue = byDay.GetValueOrDefault(d) })
                .ToList();
        }

        private Task<List<TopProduct>> GetTopProductsAsync(DateTime rangeStartUtc) =>
            context.OrderItems
                .AsNoTracking()
                .Where(i => i.Order.OrderDate >= rangeStartUtc && i.Order.Status != OrderStatus.Cancelled)
                .GroupBy(i => new { i.ProductId, i.Product.Name })
                .Select(g => new TopProduct { ProductId = g.Key.ProductId, Name = g.Key.Name, Quantity = g.Sum(i => i.Quantity) })
                .OrderByDescending(p => p.Quantity)
                .Take(5)
                .ToListAsync();

        private async Task<List<NeedsActionOrder>> GetNeedsActionAsync()
        {
            var list = await Orders
                .Where(o => o.Status == OrderStatus.Pending)
                .OrderBy(o => o.OrderDate) // oldest first: they've waited longest
                .Take(5)
                .Select(o => new NeedsActionOrder
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User.Username,
                    Total = o.Total,
                    Status = o.Status.ToString(),
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            // sql returns dates with no timezone marker; mark them UTC so the JSON ends with "Z"
            foreach (var o in list) o.OrderDate = DateTime.SpecifyKind(o.OrderDate, DateTimeKind.Utc);
            return list;
        }
    }
}
