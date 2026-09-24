using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display all products
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.PreferredSupplier)
                .ToListAsync();

            return View(products);
        }

        // Display Create Product form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // Create Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            product.IsActive = true;
            product.CurrentStock = 0;
            product.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();

                return View(product);
            }

            var skuExists = await _context.Products
                .AnyAsync(p => p.Sku == product.Sku);

            if (skuExists)
            {
                ModelState.AddModelError(
                    "Sku",
                    "A product with this SKU already exists.");

                await LoadDropdowns();

                return View(product);
            }

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // Display Edit Product form
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .FindAsync(id);

            if (product == null)
                return NotFound();

            await LoadDropdowns();

            return View(product);
        }

        // Edit Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(product);
            }

            var existingProduct = await _context.Products
                .FindAsync(product.ProductId);

            if (existingProduct == null)
                return NotFound();

            var skuExists = await _context.Products
                .AnyAsync(p =>
                    p.Sku == product.Sku &&
                    p.ProductId != product.ProductId);

            if (skuExists)
            {
                ModelState.AddModelError(
                    "Sku",
                    "A product with this SKU already exists.");

                await LoadDropdowns();

                return View(product);
            }

            existingProduct.Sku = product.Sku;
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.PreferredSupplierId = product.PreferredSupplierId;
            existingProduct.PurchasePrice = product.PurchasePrice;
            existingProduct.SellingPrice = product.SellingPrice;
            existingProduct.ReorderLevel = product.ReorderLevel;
            existingProduct.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // Delete Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .FindAsync(id);

            if (product == null)
                return NotFound();

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Load Category and Supplier dropdowns
        private async Task LoadDropdowns()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "CategoryId",
                "Name");

            ViewBag.Suppliers = new SelectList(
                await _context.Suppliers
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.Name)
                    .ToListAsync(),
                "SupplierId",
                "Name");
        }
    }
}