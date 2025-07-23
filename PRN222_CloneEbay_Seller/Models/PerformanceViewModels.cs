namespace PRN222_CloneEbay_Seller.Models
{
    public class PerformanceOverviewViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProductsSold { get; set; }
        public int ActiveProducts { get; set; }
        public decimal RevenueGrowth { get; set; }
        public int Period { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class RevenueDataViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Data { get; set; } = new List<decimal>();
        public decimal TotalRevenue { get; set; }
        public decimal AverageDaily { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class OrderStatisticsViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> OrdersData { get; set; } = new List<int>();
        public List<int> ItemsData { get; set; } = new List<int>();
        public int TotalOrders { get; set; }
        public int TotalItems { get; set; }
        public decimal AverageOrdersPerDay { get; set; }
    }

    public class TopPerformingProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class CategorySalesViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> RevenueData { get; set; } = new List<decimal>();
        public List<int> SoldData { get; set; } = new List<int>();
        public List<CategoryStatsItem> Details { get; set; } = new List<CategoryStatsItem>();
    }

    public class CategoryStatsItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public int ProductCount { get; set; }
    }

    public class PerformanceFilterViewModel
    {
        public int Days { get; set; } = 30;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Period { get; set; } = "30"; // 7, 30, 90, 365, custom
        public int SellerId { get; set; }
    }
}