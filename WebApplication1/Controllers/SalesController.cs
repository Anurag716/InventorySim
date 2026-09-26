using Inventory.Models;
using Inventory.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin,Cashier")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sales/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new SaleCreateViewModel
            {
                Customers = await _context.Customers
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.Name)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.CustomerId.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync(),

                Products = await _context.Products
                    .Where(p => p.IsActive == true)
                    .OrderBy(p => p.Name)
                    .Select(p => new ProductOptionViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.Name,
                        Sku = p.Sku,
                        PurchasePrice = p.SellingPrice,
                        CurrentStock = p.CurrentStock
                    })
                    .ToListAsync()
            };


            return View(viewModel);
        }

        // POST: Sales/Create
        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromBody] SaleCreateViewModel viewModel)
        {
            if (viewModel.Items == null || viewModel.Items.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please add at least one product to the sale."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please check the sale details."
                });
            }

            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unable to identify the logged-in user."
                });
            }

            // Prevent the same product from appearing twice in the cart.
            var duplicateProduct =
                viewModel.Items
                    .GroupBy(i => i.ProductId)
                    .Any(g => g.Count() > 1);

            if (duplicateProduct)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The same product cannot be added more than once."
                });
            }

            // Validate customer if one was selected.
            if (viewModel.CustomerId.HasValue)
            {
                var customerExists = await _context.Customers
                    .AnyAsync(c =>
                        c.CustomerId == viewModel.CustomerId.Value &&
                        c.IsActive == true);

                if (!customerExists)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "The selected customer is no longer active."
                    });
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // Get the actual products from the database.
                var productIds = viewModel.Items
                    .Select(i => i.ProductId)
                    .ToList();

                var products = await _context.Products
                    .Where(p =>
                        productIds.Contains(p.ProductId) &&
                        p.IsActive == true)
                    .ToListAsync();

                if (products.Count != productIds.Count)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "One or more selected products are unavailable."
                    });
                }

                decimal subtotal = 0;

                // Validate stock and calculate subtotal.
                foreach (var item in viewModel.Items)
                {
                    var product = products
                        .First(p => p.ProductId == item.ProductId);

                    if (item.Quantity < 1)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Invalid quantity for {product.Name}."
                        });
                    }

                    if (item.Quantity > product.CurrentStock)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message =
                                $"Insufficient stock for {product.Name}. " +
                                $"Only {product.CurrentStock} unit(s) are available."
                        });
                    }

                    // IMPORTANT:
                    // Use the selling price stored in the database.
                    subtotal +=
                        product.SellingPrice * item.Quantity;
                }

                // Calculate discount.
                decimal discountAmount =
                    Math.Round(
                        subtotal *
                        viewModel.DiscountPercentage /
                        100,
                        2);

                decimal taxableAmount =
                    Math.Max(subtotal - discountAmount, 0);

                // Calculate tax.
                decimal taxAmount =
                    Math.Round(
                        taxableAmount *
                        viewModel.TaxPercentage /
                        100,
                        2);

                decimal grandTotal =
                    Math.Round(
                        taxableAmount + taxAmount,
                        2);

                // Create the Sale record.
                var sale = new Sale
                {
                    SaleNumber =
                        $"SAL-{DateTime.Now:yyyyMMddHHmmssfff}",

                    CustomerId = viewModel.CustomerId,

                    CashierUserId = userId,

                    SaleDate = DateTime.Now,

                    Subtotal = Math.Round(subtotal, 2),

                    DiscountAmount = discountAmount,

                    TaxAmount = taxAmount,

                    GrandTotal = grandTotal,

                    Status = "PendingPayment",

                    CreatedAt = DateTime.Now
                };

                _context.Sales.Add(sale);

                // Save Sale first so SaleId is generated.
                await _context.SaveChangesAsync();

                // Create SaleItems and update inventory.
                foreach (var item in viewModel.Items)
                {
                    var product = products
                        .First(p => p.ProductId == item.ProductId);

                    decimal itemSubtotal =
                        Math.Round(
                            product.SellingPrice * item.Quantity,
                            2);

                    var saleItem = new Saleitem
                    {
                        SaleId = sale.SaleId,

                        ProductId = product.ProductId,

                        Quantity = item.Quantity,

                        UnitPrice = product.SellingPrice,

                        // We currently use only overall sale discount.
                        DiscountAmount = 0,

                        Subtotal = itemSubtotal
                    };

                    _context.Saleitems.Add(saleItem);

                    // Reduce stock.
                    product.CurrentStock -= item.Quantity;

                    // Create inventory transaction.
                    var inventoryTransaction =
                        new Inventorytransaction
                        {
                            ProductId = product.ProductId,

                            ChangeType = "Sale",

                            QuantityChange = -item.Quantity,

                            ResultingStock =
                                product.CurrentStock,

                            SaleId = sale.SaleId,

                            CreatedByUserId = userId,

                            CreatedAt = DateTime.Now,

                            Notes =
                                $"Stock reduced for sale {sale.SaleNumber}."
                        };

                    _context.Inventorytransactions.Add(
                        inventoryTransaction);
                }

                // Save SaleItems, updated stock and inventory transactions.
                await _context.SaveChangesAsync();

                // Everything succeeded.
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    saleId = sale.SaleId,
                    saleNumber = sale.SaleNumber,
                    grandTotal = sale.GrandTotal,
                    message = "Sale created successfully."
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to complete the sale. No changes were saved."
                });
            }
        }

        // POST: Sales/AddCustomerFromPos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomerFromPos(Customer customer)
        {
            customer.IsActive = true;
            customer.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please enter a valid customer name."
                });
            }

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                customerId = customer.CustomerId,
                customerName = customer.Name,
                message = "Customer added successfully."
            });
        }
    }
}