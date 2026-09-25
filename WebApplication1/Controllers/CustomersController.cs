using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(customers);
        }

        // GET: Customers/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            customer.IsActive = true;
            customer.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.CustomerId)
                .ToListAsync();

            var csv = new StringBuilder();

            // CSV header
            csv.AppendLine(
                "Customer ID,Name,Phone,Email,Address,Status,Created At,Updated At");

            foreach (var customer in customers)
            {
                csv.AppendLine(
                    $"{customer.CustomerId}," +
                    $"\"{customer.Name?.Replace("\"", "\"\"")}\"," +
                    $"\"{customer.Phone?.Replace("\"", "\"\"")}\"," +
                    $"\"{customer.Email?.Replace("\"", "\"\"")}\"," +
                    $"\"{customer.Address?.Replace("\"", "\"\"")}\"," +
                    $"{(customer.IsActive == true ? "Active" : "Inactive")}," +
                    $"{customer.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                    $"{(customer.UpdatedAt.HasValue ? customer.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(
                bytes,
                "text/csv",
                "Customers.csv");
        }

        // GET: Customers/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Customers/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            var existingCustomer = await _context.Customers
                .FindAsync(customer.CustomerId);

            if (existingCustomer == null)
                return NotFound();

            existingCustomer.Name = customer.Name;
            existingCustomer.Phone = customer.Phone;
            existingCustomer.Email = customer.Email;
            existingCustomer.Address = customer.Address;
            existingCustomer.IsActive = customer.IsActive;
            existingCustomer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Customers/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers
                .FindAsync(id);

            if (customer == null)
                return NotFound();

            _context.Customers.Remove(customer);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}