using QuickCartApi.Enums;

namespace QuickCartApi.DTOs.Admin.Dashboard
{
    public class DashboardResponse
    {
        public int PendingOrders { get; set; }
        public int StaleOrders { get; set; } // order that is pending 24 hours
        public int OrdersToday { get; set; }
        public int OrdersYesterday { get; set; }

        public decimal Revenue30d { get; set; }
        public decimal? RevenueChangePct { get; set; }
        public decimal AverageOrderValue { get; set; }

        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }

        public int NewCustomers7d { get; set; }
        public int TotalCustomers { get; set; }

        public Dictionary<OrderStatus, int> OrdersByStatus { get; set; } = [];
        public List<RestockItem> Restock { get; set; } = [];
        public List<RevenueDay> RevenueByDay { get; set; } = [];
        public List<TopProduct> TopProducts { get; set; } = [];
        public List<NeedsActionOrder> NeedsAction { get; set; } = [];
        public DashboardReviews Reviews { get; set; } = new();
    }
}
