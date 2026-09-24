using Inventory.Models;
using Inventory.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;

    public PurchasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display all purchases
        public async Task<IActionResult> Index()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Purchaseitems)
                    .ThenInclude(pi => pi.Product)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            return View(purchases);
        }

        // Display purchase details
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Purchaseitems)
                    .ThenInclude(pi => pi.Product)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase);
        }

        // Display Create Purchase form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PurchaseCreateViewModel();

            await LoadDropdowns(model);

            return View(model);
        }

        // Create Purchase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PurchaseCreateViewModel model)
        {
            // At least one product must be added
            if (model.Items == null || model.Items.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one product to the purchase.");
            }

            // Check for duplicate products
            if (model.Items != null && model.Items.Count > 0)
            {
                var duplicateProducts = model.Items
                    .GroupBy(i => i.ProductId)
                    .Any(g => g.Count() > 1);

                if (duplicateProducts)
                {
                    ModelState.AddModelError(
                        "",
                        "The same product cannot be added more than once.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model);

                return View(model);
            }

            // Generate purchase number
            var purchaseNumber = "PUR-" +
                DateTime.Now.ToString("yyyyMMddHHmmss");

            // Calculate total amount on server
            decimal totalAmount = 0;

            foreach (var item in model.Items)
            {
                totalAmount +=
                    item.Quantity * item.UnitPrice;
            }

            // Get logged-in user's ID
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var createdByUserId = int.Parse(userIdClaim.Value);

            // Create Purchase
            var purchase = new Purchase
            {
                PurchaseNumber = purchaseNumber,
                SupplierId = model.SupplierId,
                PurchaseDate = model.PurchaseDate,
                Status = "Draft",
                TotalAmount = totalAmount,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.Now
            };

            // Create Purchase Items
            foreach (var item in model.Items)
            {
                var purchaseItem = new Purchaseitem
                {
                    Purchase = purchase,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal =
                        item.Quantity * item.UnitPrice
                };

                purchase.Purchaseitems.Add(purchaseItem);
            }

            _context.Purchases.Add(purchase);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Display Edit Purchase form
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _context.Purchases
                .Include(p => p.Purchaseitems)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (purchase == null)
            {
                return NotFound();
            }

            // Completed purchases cannot be edited
            if (purchase.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only draft purchases can be edited.";

                return RedirectToAction(nameof(Index));
            }

            var model = new PurchaseEditViewModel
            {
                PurchaseId = purchase.PurchaseId,
                PurchaseNumber = purchase.PurchaseNumber,
                SupplierId = purchase.SupplierId,
                PurchaseDate = purchase.PurchaseDate
            };

            foreach (var item in purchase.Purchaseitems)
            {
                model.Items.Add(
                    new PurchaseEditItemViewModel
                    {
                        PurchaseItemId = item.PurchaseItemId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
            }

            await LoadDropdowns(model);

            return View(model);
        }

        // Save edited Purchase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            PurchaseEditViewModel model)
        {
            // At least one product must remain
            if (model.Items == null || model.Items.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please keep at least one product in the purchase.");
            }

            // Check for duplicate products
            if (model.Items != null && model.Items.Count > 0)
            {
                var duplicateProducts = model.Items
                    .GroupBy(i => i.ProductId)
                    .Any(g => g.Count() > 1);

                if (duplicateProducts)
                {
                    ModelState.AddModelError(
                        "",
                        "The same product cannot be added more than once.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model);

                return View(model);
            }

            var purchase = await _context.Purchases
                .Include(p => p.Purchaseitems)
                .FirstOrDefaultAsync(p => p.PurchaseId == model.PurchaseId);

            if (purchase == null)
            {
                return NotFound();
            }

            // Prevent editing a completed purchase
            if (purchase.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only draft purchases can be edited.";

                return RedirectToAction(nameof(Index));
            }

            // Update purchase information
            purchase.SupplierId = model.SupplierId;
            purchase.PurchaseDate = model.PurchaseDate;
            purchase.UpdatedAt = DateTime.Now;

            // IDs of items that still exist after editing
            var submittedItemIds = model.Items
                .Where(i => i.PurchaseItemId > 0)
                .Select(i => i.PurchaseItemId)
                .ToHashSet();

            // Remove purchase items that were deleted from the form
            var itemsToRemove = purchase.Purchaseitems
                .Where(pi =>
                    !submittedItemIds.Contains(pi.PurchaseItemId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                _context.Purchaseitems.Remove(item);
            }

            // Update existing items and add new items
            foreach (var item in model.Items)
            {
                if (item.PurchaseItemId > 0)
                {
                    var existingItem =
                        purchase.Purchaseitems
                            .FirstOrDefault(pi =>
                                pi.PurchaseItemId ==
                                item.PurchaseItemId);

                    if (existingItem == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "One of the purchase items could not be found.");

                        await LoadDropdowns(model);

                        return View(model);
                    }

                    existingItem.ProductId = item.ProductId;
                    existingItem.Quantity = item.Quantity;
                    existingItem.UnitPrice = item.UnitPrice;
                    existingItem.Subtotal =
                        item.Quantity * item.UnitPrice;
                }
                else
                {
                    var newItem = new Purchaseitem
                    {
                        PurchaseId = purchase.PurchaseId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal =
                            item.Quantity * item.UnitPrice
                    };

                    _context.Purchaseitems.Add(newItem);
                }
            }

            // Calculate total on the server
            decimal totalAmount = 0;

            foreach (var item in model.Items)
            {
                totalAmount +=
                    item.Quantity * item.UnitPrice;
            }

            purchase.TotalAmount = totalAmount;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Purchase updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Complete Purchase and update inventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            // Get the purchase with its items and products
            var purchase = await _context.Purchases
                .Include(p => p.Purchaseitems)
                    .ThenInclude(pi => pi.Product)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (purchase == null)
            {
                return NotFound();
            }

            // Prevent completing the same purchase twice
            if (purchase.Status == "Completed")
            {
                TempData["ErrorMessage"] =
                    "This purchase has already been completed.";

                return RedirectToAction(nameof(Index));
            }

            // Only Draft purchases can be completed
            if (purchase.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only draft purchases can be completed.";

                return RedirectToAction(nameof(Index));
            }

            // Get logged-in user's ID
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim.Value);

            // Start database transaction
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Update each product's stock
                foreach (var item in purchase.Purchaseitems)
                {
                    var product = item.Product;

                    // Increase current stock
                    product.CurrentStock += item.Quantity;

                    // Create inventory transaction
                    var inventoryTransaction =
                        new Inventorytransaction
                        {
                            ProductId = product.ProductId,
                            ChangeType = "Purchase",
                            QuantityChange = item.Quantity,
                            ResultingStock = product.CurrentStock,
                            PurchaseId = purchase.PurchaseId,
                            SaleId = null,
                            ReturnId = null,
                            Notes =
                                $"Stock added from purchase {purchase.PurchaseNumber}",
                            CreatedByUserId = userId,
                            CreatedAt = DateTime.Now
                        };

                    _context.Inventorytransactions.Add(
                        inventoryTransaction);
                }

                // Mark purchase as completed
                purchase.Status = "Completed";
                purchase.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Purchase completed and inventory updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Roll back everything if any database operation fails
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "The purchase could not be completed.";

                return RedirectToAction(nameof(Index));
            }
        }
        // Delete Draft Purchase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Get the purchase with its items
            var purchase = await _context.Purchases
                .Include(p => p.Purchaseitems)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (purchase == null)
            {
                return NotFound();
            }

            // Completed purchases cannot be deleted
            if (purchase.Status != "Draft")
            {
                TempData["ErrorMessage"] =
                    "Only draft purchases can be deleted.";

                return RedirectToAction(nameof(Index));
            }

            // Delete purchase items first
            _context.Purchaseitems.RemoveRange(
                purchase.Purchaseitems);

            // Delete the purchase
            _context.Purchases.Remove(purchase);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Purchase deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Load suppliers and products for Create
        private async Task LoadDropdowns(
            PurchaseCreateViewModel model)
        {
            model.Suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .Select(s =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.Name
                    })
                .ToListAsync();

            model.Products = await _context.Products
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.Name)
                .Select(p => new ProductOptionViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Sku = p.Sku,
                    PurchasePrice = p.PurchasePrice
                })
                .ToListAsync();
        }

        // Load suppliers and products for Edit
        private async Task LoadDropdowns(
            PurchaseEditViewModel model)
        {
            model.Suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .Select(s =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.Name
                    })
                .ToListAsync();

            model.Products = await _context.Products
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.Name)
                .Select(p => new ProductOptionViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Sku = p.Sku,
                    PurchasePrice = p.PurchasePrice
                })
                .ToListAsync();
        }
    }


}
