using Inventory.Models;
using Inventory.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Models;

namespace Inventory.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = User.IsInRole("Admin");
            var isCashier = User.IsInRole("Cashier");

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var model = new DashboardViewModel
            {
                UserName = User.Identity?.Name ?? "User",
                Role = isAdmin ? "Admin" : "Cashier"
            };

            // =====================================================
            // COMMON DASHBOARD DATA
            // =====================================================

            model.TotalProducts = await _context.Products
                .CountAsync(p => p.IsActive == true);

            model.LowStockProducts = await _context.Products
                .CountAsync(p =>
                    p.IsActive == true &&
                    p.CurrentStock <= p.ReorderLevel);

            // =====================================================
            // ADMIN DASHBOARD
            // =====================================================

            if (isAdmin)
            {
                model.TodaySales = await _context.Sales
                    .CountAsync(s =>
                        s.Status == "Paid" &&
                        s.SaleDate >= today &&
                        s.SaleDate < tomorrow);

                model.TodayRevenue = await _context.Sales
                    .Where(s =>
                        s.Status == "Paid" &&
                        s.SaleDate >= today &&
                        s.SaleDate < tomorrow)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0;

                model.RecentSales = await _context.Sales
                    .Include(s => s.Customer)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToListAsync();

                model.LowStockItems = await _context.Products
                    .Where(p =>
                        p.IsActive == true &&
                        p.CurrentStock <= p.ReorderLevel)
                    .OrderBy(p => p.CurrentStock)
                    .Take(5)
                    .ToListAsync();
            }

            // =====================================================
            // CASHIER DASHBOARD
            // =====================================================

            if (isCashier)
            {
                model.TodaySales = await _context.Sales
                    .CountAsync(s =>
                        s.CashierUserId == userId &&
                        s.Status == "Paid" &&
                        s.SaleDate >= today &&
                        s.SaleDate < tomorrow);

                model.TodayRevenue = await _context.Sales
                    .Where(s =>
                        s.CashierUserId == userId &&
                        s.Status == "Paid" &&
                        s.SaleDate >= today &&
                        s.SaleDate < tomorrow)
                    .SumAsync(s => (decimal?)s.GrandTotal) ?? 0;

                model.MyRecentSales = await _context.Sales
                    .Include(s => s.Customer)
                    .Where(s => s.CashierUserId == userId)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToListAsync();

                model.LowStockItems = await _context.Products
                    .Where(p =>
                        p.IsActive == true &&
                        p.CurrentStock <= p.ReorderLevel)
                    .OrderBy(p => p.CurrentStock)
                    .Take(5)
                    .ToListAsync();
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}