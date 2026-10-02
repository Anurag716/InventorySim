using System.Security.Claims;
using Inventory.Models;
using Inventory.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin,Cashier")]
    public class ReturnsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReturnsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Returns/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var returns = await _context.Returns
                .Include(r => r.Sale)
                    .ThenInclude(s => s.Customer)
                .Include(r => r.ProcessedByUser)
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();

            if (User.IsInRole("Cashier"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized();
                }

                returns = returns
                    .Where(r => r.ProcessedByUserId == userId)
                    .ToList();
            }

            return View(returns);
        }


        // GET: Returns/Details/5
        [HttpGet]
        public async Task<IActionResult> GetReturnDetails(int id)
        {
            var returnRecord = await _context.Returns
                .Include(r => r.Sale)
                    .ThenInclude(s => s.Customer)
                .Include(r => r.ProcessedByUser)
                .Include(r => r.Returnitems)
                    .ThenInclude(ri => ri.SaleItem)
                        .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(r => r.ReturnId == id);

            if (returnRecord == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Return not found."
                });
            }

            if (User.IsInRole("Cashier"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized();
                }

                if (returnRecord.ProcessedByUserId != userId)
                {
                    return Forbid();
                }
            }

            return Ok(new
            {
                success = true,

                returnId = returnRecord.ReturnId,
                returnNumber = returnRecord.ReturnNumber,
                returnDate = returnRecord.ReturnDate,
                saleNumber = returnRecord.Sale?.SaleNumber,
                customerName = returnRecord.Sale?.Customer?.Name
                               ?? "Walk-in Customer",
                refundAmount = returnRecord.RefundAmount,
                status = returnRecord.Status,
                reason = returnRecord.Reason,
                processedBy = returnRecord.ProcessedByUser?.FullName,

                items = returnRecord.Returnitems.Select(ri => new
                {
                    productName = ri.SaleItem?.Product?.Name,
                    sku = ri.SaleItem?.Product?.Sku,
                    quantity = ri.Quantity,
                    refundAmount = ri.RefundAmount
                })
            });
        }

        // GET: Returns/ExportCsv
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var returns = await _context.Returns
                .Include(r => r.Sale)
                    .ThenInclude(s => s.Customer)
                .Include(r => r.ProcessedByUser)
                .Include(r => r.Returnitems)
                    .ThenInclude(ri => ri.SaleItem)
                        .ThenInclude(si => si.Product)
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();

            csv.AppendLine(
                "Return Number,Sale Number,Customer,Return Date,Refund Amount,Status,Processed By,Reason,Products"
            );

            foreach (var returnRecord in returns)
            {
                var products = string.Join(
                    " | ",
                    returnRecord.Returnitems.Select(ri =>
                        $"{ri.SaleItem.Product.Name} x{ri.Quantity}"
                    )
                );

                var customer =
                    returnRecord.Sale?.Customer?.Name
                    ?? "Walk-in Customer";

                csv.AppendLine(string.Join(",",
                    CsvEscape(returnRecord.ReturnNumber),
                    CsvEscape(returnRecord.Sale?.SaleNumber ?? ""),
                    CsvEscape(customer),
                    CsvEscape(returnRecord.ReturnDate.ToString("yyyy-MM-dd HH:mm:ss")),
                    returnRecord.RefundAmount.ToString("0.00"),
                    CsvEscape(returnRecord.Status),
                    CsvEscape(returnRecord.ProcessedByUser?.FullName ?? ""),
                    CsvEscape(returnRecord.Reason ?? ""),
                    CsvEscape(products)
                ));
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

            return File(
                bytes,
                "text/csv",
                $"Returns_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            );
        }

        private static string CsvEscape(string value)
        {
            if (value.Contains(',') ||
                value.Contains('"') ||
                value.Contains('\n') ||
                value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }


        // get: /Returns/Create?saleId=123

        [HttpGet]
        public async Task<IActionResult> Create(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Saleitems)
                    .ThenInclude(si => si.Product)
                .Include(s => s.Returns)
                    .ThenInclude(r => r.Returnitems)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (sale == null)
                return NotFound();

            // Only paid sales can be returned.
            if (sale.Status != "Paid")
                return BadRequest("Only paid sales can be returned.");

            // Cashier can only process returns for their own sales.
            if (User.IsInRole("Cashier"))
            {
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized();

                if (!int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized();

                if (sale.CashierUserId != userId)
                    return Forbid();
            }

            var viewModel = new ReturnCreateViewModel
            {
                SaleId = sale.SaleId,
                SaleNumber = sale.SaleNumber,
                CustomerName =
                    sale.Customer?.Name ?? "Walk-in Customer",
                SaleSubtotal = sale.Subtotal,
                SaleDiscountAmount = sale.DiscountAmount,
                SaleDate = sale.SaleDate
            };

            foreach (var saleItem in sale.Saleitems)
            {
                var alreadyReturnedQuantity = sale.Returns
                    .SelectMany(r => r.Returnitems)
                    .Where(ri => ri.SaleItemId == saleItem.SaleItemId)
                    .Sum(ri => ri.Quantity);

                var returnableQuantity =
                    saleItem.Quantity - alreadyReturnedQuantity;

                viewModel.Items.Add(new ReturnItemViewModel
                {
                    SaleItemId = saleItem.SaleItemId,
                    ProductId = saleItem.ProductId,
                    ProductName = saleItem.Product.Name,
                    Sku = saleItem.Product.Sku,
                    PurchasedQuantity = saleItem.Quantity,
                    AlreadyReturnedQuantity =
                        alreadyReturnedQuantity,
                    ReturnableQuantity =
                        Math.Max(0, returnableQuantity),
                    ReturnQuantity = 0,
                    UnitPrice = saleItem.UnitPrice,
                    RefundAmount = 0
                });
            }

            return View(viewModel);
        }

        // post: /Returns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReturnCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                return BadRequest(new
                {
                    success = false,
                    message = errors.FirstOrDefault()
                               ?? "Invalid return information."
                });
            }

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var sale = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.Saleitems)
                        .ThenInclude(si => si.Product)
                    .Include(s => s.Returns)
                        .ThenInclude(r => r.Returnitems)
                    .FirstOrDefaultAsync(s =>
                        s.SaleId == viewModel.SaleId);

                if (sale == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Sale not found."
                    });
                }

                if (sale.Status != "Paid")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Only paid sales can be returned."
                    });
                }

                // Cashier can only process returns for their own sales.
                if (User.IsInRole("Cashier") &&
                    sale.CashierUserId != userId)
                {
                    return Forbid();
                }

                if (viewModel.Items == null ||
                    viewModel.Items.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No return items were selected."
                    });
                }

                var selectedItems = viewModel.Items
                    .Where(i => i.ReturnQuantity > 0)
                    .ToList();

                if (!selectedItems.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please select at least one item to return."
                    });
                }

                // Prevent the same SaleItem from appearing multiple times.
                if (selectedItems
                    .GroupBy(i => i.SaleItemId)
                    .Any(g => g.Count() > 1))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Duplicate return items are not allowed."
                    });
                }

                decimal totalRefund = 0;

                var returnItems = new List<Returnitem>();

                foreach (var selectedItem in selectedItems)
                {
                    var saleItem = sale.Saleitems
                        .FirstOrDefault(si =>
                            si.SaleItemId == selectedItem.SaleItemId);

                    if (saleItem == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "One of the selected sale items was not found."
                        });
                    }

                    var alreadyReturnedQuantity = sale.Returns
                        .SelectMany(r => r.Returnitems)
                        .Where(ri =>
                            ri.SaleItemId == saleItem!.SaleItemId)
                        .Sum(ri => ri.Quantity);

                    var returnableQuantity =
                        saleItem!.Quantity - alreadyReturnedQuantity;

                    if (selectedItem.ReturnQuantity <= 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Return quantity must be greater than zero."
                        });
                    }

                    if (selectedItem.ReturnQuantity >
                        returnableQuantity)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message =
                                $"Cannot return more than the remaining " +
                                $"returnable quantity for {saleItem.Product.Name}."
                        });
                    }

                    /*
                     * Calculate this item's share of the sale-level discount.
                     *
                     * Example:
                     *
                     * Sale subtotal = 54,000
                     * Sale discount = 4,000
                     * Item value    = 52,000
                     *
                     * Item discount share:
                     * 4,000 × (52,000 / 54,000)
                     */

                    decimal itemTotal =
                        saleItem.UnitPrice * saleItem.Quantity;

                    decimal itemDiscountShare = 0;

                    if (sale.Subtotal > 0)
                    {
                        itemDiscountShare =
                            sale.DiscountAmount *
                            (itemTotal / sale.Subtotal);
                    }

                    decimal itemDiscountPerUnit =
                        saleItem.Quantity > 0
                            ? itemDiscountShare / saleItem.Quantity
                            : 0;

                    decimal refundPerUnit =
                        saleItem.UnitPrice -
                        itemDiscountPerUnit;

                    decimal itemRefund =
                        refundPerUnit *
                        selectedItem.ReturnQuantity;

                    itemRefund =
                        Math.Round(
                            itemRefund,
                            2,
                            MidpointRounding.AwayFromZero);

                    totalRefund += itemRefund;

                    returnItems.Add(new Returnitem
                    {
                        SaleItemId = saleItem.SaleItemId,
                        Quantity = selectedItem.ReturnQuantity,
                        RefundAmount = itemRefund
                    });
                }

                if (totalRefund <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Refund amount must be greater than zero."
                    });
                }

                var returnNumber =
                    $"RET-{DateTime.Now:yyyyMMddHHmmssfff}";

                var returnRecord = new Return
                {
                    ReturnNumber = returnNumber,
                    SaleId = sale.SaleId,
                    ReturnDate = DateTime.Now,
                    ProcessedByUserId = userId,
                    RefundAmount = totalRefund,
                    Status = "Completed",
                    Reason = viewModel.Reason
                };

                _context.Returns.Add(returnRecord);

                await _context.SaveChangesAsync();

                foreach (var returnItem in returnItems)
                {
                    returnItem.ReturnId =
                        returnRecord.ReturnId;

                    _context.Returnitems.Add(returnItem);
                }

                await _context.SaveChangesAsync();

                /*
                 * Increase product stock and create inventory transactions.
                 */

                foreach (var selectedItem in selectedItems)
                {
                    var saleItem = sale.Saleitems
                        .First(si =>
                            si.SaleItemId == selectedItem.SaleItemId);

                    var product = saleItem.Product;

                    product.CurrentStock +=
                        selectedItem.ReturnQuantity;

                    var inventoryTransaction =
                        new Inventorytransaction
                        {
                            ProductId = product.ProductId,
                            ChangeType = "Return",
                            QuantityChange =
                                selectedItem.ReturnQuantity,
                            ResultingStock =
                                product.CurrentStock,
                            ReturnId =
                                returnRecord.ReturnId,
                            Notes =
                                $"Stock returned from sale {sale.SaleNumber}.",
                            CreatedByUserId = userId,
                            CreatedAt = DateTime.Now
                        };

                    _context.Inventorytransactions.Add(
                        inventoryTransaction);
                }

                /*
                 * Record the cash refund.
                 */

                var payment = new Payment
                {
                    SaleId = sale.SaleId,
                    PaymentType = "ReturnRefund",
                    PaymentMethod = "Cash",
                    Amount = totalRefund,
                    PaymentDate = DateTime.Now,
                    Status = "Completed",
                    ReferenceNumber = null,
                    ReturnId = returnRecord.ReturnId
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    returnId = returnRecord.ReturnId,
                    returnNumber = returnRecord.ReturnNumber,
                    saleNumber = sale.SaleNumber,
                    refundAmount = totalRefund,
                    message = "Return processed successfully."
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "An unexpected error occurred while processing the return."
                });
            }
        }
    }
}