using Inventory.Models;

namespace Inventory.ViewModels
{
    public class DashboardViewModel
    {
        // Common information
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Summary statistics
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TodaySales { get; set; }
        public decimal TodayRevenue { get; set; }

        // Admin dashboard
        public List<Sale> RecentSales { get; set; } = new();

        // Cashier dashboard
        public List<Sale> MyRecentSales { get; set; } = new();

        // Stock alerts
        public List<Product> LowStockItems { get; set; } = new();
    }
}
