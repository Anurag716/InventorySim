using Inventory.Models;
using Inventory.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.Security.Claims;
using System.Text;

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

        // GET: Sales
        [HttpGet]
        public async Task<IActionResult> Index(
                    string? search,
                    string? status,
                    DateTime? fromDate,
                    DateTime? toDate)
        {
            var query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.CashierUser)
                .Include(s => s.Payments)
                .Include(s => s.Invoices)
                .AsQueryable();

            // Cashier can only see their own sales.
            if (User.IsInRole("Cashier"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized();

                if (!int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized();

                query = query.Where(s => s.CashierUserId == userId);
            }


            // Search by sale number or customer name.
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(s =>
                    s.SaleNumber.Contains(search) ||
                    (s.Customer != null &&
                     s.Customer.Name.Contains(search)));
            }

            // Filter by status.
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            // Filter by starting date.
            if (fromDate.HasValue)
            {
                query = query.Where(s =>
                    s.SaleDate >= fromDate.Value.Date);
            }

            // Filter by ending date.
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                query = query.Where(s =>
                    s.SaleDate < endDate);
            }

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(sales);
        }

        // GET: Sales/Details/5
        [HttpGet]
        public async Task<IActionResult> GetSaleDetails(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.CashierUser)
                .Include(s => s.Saleitems)
                    .ThenInclude(si => si.Product)
                .Include(s => s.Payments)
                .Include(s => s.Invoices)
                .FirstOrDefaultAsync(s => s.SaleId == id);

            if (sale == null)
                return NotFound();

            // Cashier can only view their own sales.
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

            return Json(new
            {
                saleId = sale.SaleId,
                saleNumber = sale.SaleNumber,
                saleDate = sale.SaleDate,
                status = sale.Status,

                customer = sale.Customer == null
                    ? null
                    : new
                    {
                        name = sale.Customer.Name,
                        phone = sale.Customer.Phone,
                        email = sale.Customer.Email
                    },

                cashier = sale.CashierUser.FullName,

                items = sale.Saleitems.Select(item => new
                {
                    productName = item.Product.Name,
                    sku = item.Product.Sku,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                    discountAmount = item.DiscountAmount,
                    subtotal = item.Subtotal
                }),

                subtotal = sale.Subtotal,
                discountAmount = sale.DiscountAmount,
                taxAmount = sale.TaxAmount,
                grandTotal = sale.GrandTotal,

                payment = sale.Payments
                    .OrderByDescending(p => p.PaymentId)
                    .Select(p => new
                    {
                        paymentMethod = p.PaymentMethod,
                        amount = p.Amount,
                        paymentDate = p.PaymentDate,
                        status = p.Status
                    })
                    .FirstOrDefault(),

                invoice = sale.Invoices
                    .OrderByDescending(i => i.InvoiceId)
                    .Select(i => new
                    {
                        invoiceId = i.InvoiceId,
                        invoiceNumber = i.InvoiceNumber,
                        invoiceDate = i.InvoiceDate
                    })
                    .FirstOrDefault()
            });
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


        // GET: Sales/Payment/
        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Saleitems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.SaleId == id);

            if (sale == null)
                return NotFound();

            if (sale.Status != "PendingPayment")
                return BadRequest("This sale is not awaiting payment.");

            var viewModel = new PaymentViewModel
            {
                SaleId = sale.SaleId,
                Amount = sale.GrandTotal
            };

            ViewBag.Sale = sale;

            return View(viewModel);
        }
        // POST: Sales/ConfirmPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(
    [FromBody] PaymentViewModel viewModel)
        {
            if (viewModel.SaleId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid sale."
                });
            }

            if (string.IsNullOrWhiteSpace(viewModel.PaymentMethod))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select a payment method."
                });
            }

            if (viewModel.PaymentMethod != "Cash" &&
                viewModel.PaymentMethod != "QR")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid payment method."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = await _context.Sales
                    .Include(s => s.Customer)
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

                if (sale.Status != "PendingPayment")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "This sale has already been processed."
                    });
                }

                /*
                 * Always use the amount stored in the database.
                 * Do not trust the amount sent by the browser.
                 */
                decimal amount = sale.GrandTotal;

                var payment = new Payment
                {
                    SaleId = sale.SaleId,
                    PaymentType = "SalePayment",
                    PaymentMethod = viewModel.PaymentMethod,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    Status = "Completed",
                    ReferenceNumber = null,
                    ReturnId = null
                };

                _context.Payments.Add(payment);

                sale.Status = "Paid";
                sale.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var invoice = new Invoice
                {
                    InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}",
                    SaleId = sale.SaleId,
                    InvoiceDate = DateTime.Now,
                    TotalAmount = sale.GrandTotal
                };

                _context.Invoices.Add(invoice);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    saleId = sale.SaleId,
                    saleNumber = sale.SaleNumber,
                    amount = sale.GrandTotal,
                    paymentMethod = payment.PaymentMethod,
                    customerId = sale.CustomerId,
                    customerName = sale.Customer?.Name,
                    customerPhone = sale.Customer?.Phone,
                    invoiceId = invoice.InvoiceId,
                    invoiceNumber = invoice.InvoiceNumber,
                    message = "Payment completed successfully."
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Unable to process the payment. No changes were saved."
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

        // GET: Sales/InvoicePdf/5
        // GET: Sales/InvoicePdf/5
        [HttpGet]
        public async Task<IActionResult> InvoicePdf(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Sale)
                    .ThenInclude(s => s.Customer)
                .Include(i => i.Sale)
                    .ThenInclude(s => s.Saleitems)
                        .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound();

            var sale = invoice.Sale;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.DefaultTextStyle(x =>
                        x.FontSize(10));

                    // Header
                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("INVENTRA")
                                .FontSize(24)
                                .Bold();

                            column.Item()
                                .Text("Inventory Management System")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    // Content
                    page.Content()
                        .PaddingTop(20)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            column.Item()
                                .Text("INVOICE")
                                .FontSize(20)
                                .Bold();

                            column.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Column(left =>
                                        {
                                            left.Item()
                                                .Text(
                                                    $"Invoice No: {invoice.InvoiceNumber}");

                                            left.Item()
                                                .Text(
                                                    $"Sale No: {sale.SaleNumber}");

                                            left.Item()
                                                .Text(
                                                    $"Date: {invoice.InvoiceDate:dd-MM-yyyy HH:mm}");
                                        });

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Column(right =>
                                        {
                                            right.Item()
                                                .Text("Bill To")
                                                .Bold();

                                            right.Item()
                                                .Text(
                                                    sale.Customer?.Name
                                                    ?? "Walk-in Customer");

                                            if (!string.IsNullOrWhiteSpace(
                                                sale.Customer?.Phone))
                                            {
                                                right.Item()
                                                    .Text(
                                                        $"Phone: {sale.Customer.Phone}");
                                            }
                                        });
                                });

                            // Products table
                            column.Item()
                                .PaddingTop(10)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCell)
                                            .Text("Product");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .AlignCenter()
                                            .Text("Qty");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .AlignRight()
                                            .Text("Unit Price");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .AlignRight()
                                            .Text("Total");
                                    });

                                    foreach (var item in sale.Saleitems)
                                    {
                                        table.Cell()
                                            .Element(BodyCell)
                                            .Text(item.Product.Name);

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignCenter()
                                            .Text(
                                                item.Quantity.ToString());

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignRight()
                                            .Text(
                                                $"₹{item.UnitPrice:N2}");

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignRight()
                                            .Text(
                                                $"₹{item.Subtotal:N2}");
                                    }
                                });

                            // Amount summary
                            column.Item()
                                .PaddingTop(15)
                                .AlignRight()
                                .Width(250)
                                .Column(summary =>
                                {
                                    summary.Item()
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text("Subtotal");

                                            row.ConstantItem(100)
                                                .AlignRight()
                                                .Text(
                                                    $"₹{sale.Subtotal:N2}");
                                        });

                                    summary.Item()
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text("Discount");

                                            row.ConstantItem(100)
                                                .AlignRight()
                                                .Text(
                                                    $"- ₹{sale.DiscountAmount:N2}");
                                        });

                                    summary.Item()
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text("Tax");

                                            row.ConstantItem(100)
                                                .AlignRight()
                                                .Text(
                                                    $"₹{sale.TaxAmount:N2}");
                                        });

                                    summary.Item()
                                        .PaddingTop(5)
                                        .BorderTop(1)
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text("Grand Total")
                                                .Bold();

                                            row.ConstantItem(100)
                                                .AlignRight()
                                                .Text(
                                                    $"₹{sale.GrandTotal:N2}")
                                                .Bold();
                                        });
                                });

                            column.Item()
                                .PaddingTop(20)
                                .Text("Payment Status: PAID")
                                .Bold();

                            column.Item()
                                .Text(
                                    "Thank you for shopping with Inventra!");
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated by Inventra");
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                $"{invoice.InvoiceNumber}.pdf");
        }

        // Helper methods for styling table cells
        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .Padding(5);
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        // sales Export to CSV
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.CashierUser)
                .Include(s => s.Payments)
                .Include(s => s.Invoices)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var csv = new StringBuilder();

            csv.AppendLine(
                "Sale ID,Sale Number,Customer,Cashier,Sale Date,Subtotal,Discount,Tax,Grand Total,Status,Payment Method,Invoice Number");

            foreach (var sale in sales)
            {
                var payment = sale.Payments
                    .OrderByDescending(p => p.PaymentId)
                    .FirstOrDefault();

                var invoice = sale.Invoices
                    .OrderByDescending(i => i.InvoiceId)
                    .FirstOrDefault();

                var customerName =
                    sale.Customer?.Name ?? "Walk-in Customer";

                var paymentMethod =
                    payment?.PaymentMethod ?? "";

                var invoiceNumber =
                    invoice?.InvoiceNumber ?? "";

                csv.AppendLine(
                    $"{sale.SaleId}," +
                    $"\"{sale.SaleNumber.Replace("\"", "\"\"")}\"," +
                    $"\"{customerName.Replace("\"", "\"\"")}\"," +
                    $"\"{sale.CashierUser.FullName.Replace("\"", "\"\"")}\"," +
                    $"{sale.SaleDate:yyyy-MM-dd HH:mm:ss}," +
                    $"{sale.Subtotal:F2}," +
                    $"{sale.DiscountAmount:F2}," +
                    $"{sale.TaxAmount:F2}," +
                    $"{sale.GrandTotal:F2}," +
                    $"\"{sale.Status.Replace("\"", "\"\"")}\"," +
                    $"\"{paymentMethod.Replace("\"", "\"\"")}\"," +
                    $"\"{invoiceNumber.Replace("\"", "\"\"")}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(
                bytes,
                "text/csv",
                "Sales.csv");
        }
    }
}