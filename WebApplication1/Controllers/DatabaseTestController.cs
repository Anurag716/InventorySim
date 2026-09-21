using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Controllers
{
    public class DatabaseTestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatabaseTestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            try
            {
                var roles = _context.Roles
                            .Include(r => r.Users)
                             .ToList();

                ViewBag.Status = "SUCCESS";
                ViewBag.Message = $"EF Core successfully retrieved {roles.Count} roles.";

                return View(roles);
            }
            catch (Exception ex)
            {
                ViewBag.Status = "ERROR";
                ViewBag.Message = ex.Message;

                return View();
            }
        }
    }
}