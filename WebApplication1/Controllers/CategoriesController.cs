using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display all categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .ToListAsync();

            return View(categories);
        }
        //Create a new category
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            category.IsActive = true;
            category.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var nameExists = await _context.Categories
                .AnyAsync(c => c.Name == category.Name);

            if (nameExists)
            {
                ModelState.AddModelError(
                    "Name",
                    "A category with this name already exists.");

                return View(category);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // Edit an existing category
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            var existingCategory = await _context.Categories
                .FindAsync(category.CategoryId);

            if (existingCategory == null)
            {
                return NotFound();
            }

            var nameExists = await _context.Categories
                .AnyAsync(c =>
                    c.Name == category.Name &&
                    c.CategoryId != category.CategoryId);

            if (nameExists)
            {
                TempData["Error"] =
                    "A category with this name already exists.";

                return RedirectToAction(nameof(Index));
            }

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // Delete a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}