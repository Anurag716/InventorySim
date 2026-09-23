using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display all suppliers
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers
                .ToListAsync();

            return View(suppliers);
        }

        // Display Create Supplier form
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Create Supplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            supplier.IsActive = true;
            supplier.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return View(supplier);
            }

            var nameExists = await _context.Suppliers
                .AnyAsync(s => s.Name == supplier.Name);

            if (nameExists)
            {
                ModelState.AddModelError(
                    "Name",
                    "A supplier with this name already exists.");

                return View(supplier);
            }

            _context.Suppliers.Add(supplier);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        //Edit Supplier 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Supplier supplier)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            var existingSupplier = await _context.Suppliers
                .FindAsync(supplier.SupplierId);

            if (existingSupplier == null)
            {
                return NotFound();
            }

            var nameExists = await _context.Suppliers
                .AnyAsync(s =>
                    s.Name == supplier.Name &&
                    s.SupplierId != supplier.SupplierId);

            if (nameExists)
            {
                TempData["Error"] =
                    "A supplier with this name already exists.";

                return RedirectToAction(nameof(Index));
            }

            existingSupplier.Name = supplier.Name;
            existingSupplier.ContactPerson = supplier.ContactPerson;
            existingSupplier.Phone = supplier.Phone;
            existingSupplier.Email = supplier.Email;
            existingSupplier.Address = supplier.Address;
            existingSupplier.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // Delete Supplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers
                .FindAsync(id);

            if (supplier == null)
            {
                return NotFound();
            }

            _context.Suppliers.Remove(supplier);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
  