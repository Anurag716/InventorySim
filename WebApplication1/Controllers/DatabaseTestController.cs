using Inventory.Models;
using Microsoft.AspNetCore.Mvc;

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
                bool connected = _context.Database.CanConnect();

                if (connected)
                {
                    ViewBag.Status = "SUCCESS";
                    ViewBag.Message = "Database connection successful!";
                }
                else
                {
                    ViewBag.Status = "FAILED";
                    ViewBag.Message = "Could not connect to the database.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Status = "ERROR";
                ViewBag.Message = ex.Message;
            }

            return View();
        }
    }
}