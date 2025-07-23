using System.ComponentModel.DataAnnotations;

namespace PRN222_CloneEbay_Seller.Models
{
    public class OverviewDashboardViewModel
    {
        public ShopOverviewViewModel ShopOverview { get; set; } = new();
        public ShopStatsViewModel ShopStats { get; set; } = new();
        public PerformanceMetricsViewModel PerformanceMetrics { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    }

    public class ShopOverviewViewModel
    {
        public string ShopName { get; set; } = string.Empty;
        public string ShopDescription { get; set; } = string.Empty;
        public string ShopAvatar { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public int TotalFollowers { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
    }

    public class ShopStatsViewModel
    {
        public int TotalProducts { get; set; }
        public int ActiveListings { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int TotalOrders { get; set; }
    }

    public class PerformanceMetricsViewModel
    {
        public decimal SuccessfulOrdersPercentage { get; set; }
        public decimal RefundedOrdersPercentage { get; set; }
        public decimal CancelledOrdersPercentage { get; set; }
        public int TotalViews { get; set; }
        public int TotalWatchers { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int ReturnRequests { get; set; }
        public int ActiveDisputes { get; set; }
    }

    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalSales { get; set; }
        public int TotalViews { get; set; }
        public decimal Revenue { get; set; }
        public decimal Price { get; set; }
        public int InventoryCount { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty; // "sale", "order", "review", "refund", etc.
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string RelatedEntityId { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}