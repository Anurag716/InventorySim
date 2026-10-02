using Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Table;
using OfficeOpenXml.Style;

namespace Inventory.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesReport(
                DateTime? fromDate,
                DateTime? toDate)
        {
            var salesQuery = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.CashierUser)
                .Include(s => s.Payments)
                .AsQueryable();

            // From date
            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                salesQuery = salesQuery.Where(
                    s => s.SaleDate >= startDate
                );
            }

            // To date
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                salesQuery = salesQuery.Where(
                    s => s.SaleDate < endDate
                );
            }

            var sales = await salesQuery
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new
                {
                    saleNumber = s.SaleNumber,
                    date = s.SaleDate,
                    customer = s.Customer != null
                        ? s.Customer.Name
                        : "Walk-in Customer",

                    cashier = s.CashierUser.FullName,

                    subtotal = s.Subtotal,
                    discount = s.DiscountAmount,
                    tax = s.TaxAmount,
                    grandTotal = s.GrandTotal,

                    paymentMethod = s.Payments
                        .Where(p => p.PaymentType == "SalePayment")
                        .Select(p => p.PaymentMethod)
                        .FirstOrDefault(),

                    status = s.Status
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = sales
            });
        }


        // GET: products/report
        // GET: products/report
        [HttpGet]
        public async Task<IActionResult> GetProductsReport(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var saleItemsQuery = _context.Saleitems
                .Include(si => si.Product)
                .Include(si => si.Sale)
                .Where(si => si.Sale.Status == "Paid");

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                saleItemsQuery = saleItemsQuery
                    .Where(si => si.Sale.SaleDate >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                saleItemsQuery = saleItemsQuery
                    .Where(si => si.Sale.SaleDate < endDate);
            }

            var products = await saleItemsQuery
                .GroupBy(si => new
                {
                    si.ProductId,
                    si.Product.Name
                })
                .Select(g => new
                {
                    productId = g.Key.ProductId,
                    productName = g.Key.Name,
                    quantitySold = g.Sum(x => x.Quantity),
                    salesAmount = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.quantitySold)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = products
            });
        }

        // GET: Report/Returns 
        [HttpGet]
        public async Task<IActionResult> GetReturnsReport(
    DateTime? fromDate,
    DateTime? toDate)
        {
            var returnsQuery = _context.Returns
                .Include(r => r.Sale)
                .Include(r => r.ProcessedByUser)
                .Where(r => r.Status == "Completed");

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                returnsQuery = returnsQuery
                    .Where(r => r.ReturnDate >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                returnsQuery = returnsQuery
                    .Where(r => r.ReturnDate < endDate);
            }

            var returns = await returnsQuery
                .OrderByDescending(r => r.ReturnDate)
                .Select(r => new
                {
                    returnId = r.ReturnId,
                    returnNumber = r.ReturnNumber,
                    saleNumber = r.Sale.SaleNumber,
                    returnDate = r.ReturnDate,
                    refundAmount = r.RefundAmount,
                    processedBy = r.ProcessedByUser.FullName,
                    reason = r.Reason
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = returns
            });
        }

        // GET: Report/Customers
        [HttpGet]
        public async Task<IActionResult> GetCustomersReport(
    DateTime? fromDate,
    DateTime? toDate)
        {
            var salesQuery = _context.Sales
                .Include(s => s.Customer)
                .Where(s =>
                    s.Status == "Paid" &&
                    s.CustomerId != null);

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                salesQuery = salesQuery
                    .Where(s => s.SaleDate >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                salesQuery = salesQuery
                    .Where(s => s.SaleDate < endDate);
            }

            var customers = await salesQuery
                .GroupBy(s => new
                {
                    s.CustomerId,
                    CustomerName = s.Customer!.Name
                })
                .Select(g => new
                {
                    customerId = g.Key.CustomerId,
                    customerName = g.Key.CustomerName,
                    salesCount = g.Count(),
                    totalSpent = g.Sum(s => s.GrandTotal),
                    averageOrderValue = g.Average(s => s.GrandTotal)
                })
                .OrderByDescending(x => x.totalSpent)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = customers
            });
        }

        //GET: Report/Income
        [HttpGet]
        public async Task<IActionResult> GetIncomeReport(
    DateTime? fromDate,
    DateTime? toDate)
        {
            var salesQuery = _context.Sales
                .Where(s => s.Status == "Paid");

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                salesQuery = salesQuery
                    .Where(s => s.SaleDate >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                salesQuery = salesQuery
                    .Where(s => s.SaleDate < endDate);
            }

            var income = await salesQuery
                .OrderBy(s => s.SaleDate)
                .Select(s => new
                {
                    saleNumber = s.SaleNumber,
                    saleDate = s.SaleDate,
                    subtotal = s.Subtotal,
                    discount = s.DiscountAmount,
                    tax = s.TaxAmount,
                    grandTotal = s.GrandTotal
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = income
            });
        }


        // GET: Report/inventory
        [HttpGet]
        public async Task<IActionResult> GetInventoryReport()
        {
            var products = await _context.Products
                .Where(p => p.IsActive == true)
                .Select(p => new
                {
                    productId = p.ProductId,
                    productName = p.Name,
                    sku = p.Sku,
                    currentStock = p.CurrentStock,
                    reorderLevel = p.ReorderLevel,
                    status = p.CurrentStock == 0
                        ? "Out of Stock"
                        : p.CurrentStock <= p.ReorderLevel
                            ? "Low Stock"
                            : "In Stock"
                })
                .OrderBy(p => p.currentStock)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = products
            });
        }

        // GET: Report/export
        [HttpGet]
        public async Task<IActionResult> ExportExcel(
     string reportType,
     DateTime? fromDate,
     DateTime? toDate)
        {
            if (string.IsNullOrWhiteSpace(reportType))
            {
                return BadRequest("Report type is required.");
            }

            using var package = new ExcelPackage();

            var worksheet = package.Workbook.Worksheets.Add(reportType);

            // ---------------------------------------------------------
            // Report Header
            // ---------------------------------------------------------

            worksheet.Cells["A1"].Value = $"{reportType} Report";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 16;

            worksheet.Cells["A2"].Value =
                $"From: {fromDate?.ToString("dd-MM-yyyy") ?? "All"}" +
                $"   To: {toDate?.ToString("dd-MM-yyyy") ?? "All"}";

            worksheet.Cells["A2"].Style.Font.Italic = true;


            // =========================================================
            // SALES REPORT
            // =========================================================

            if (reportType == "Sales")
            {
                var salesQuery = _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.CashierUser)
                    .Include(s => s.Payments)
                    .AsQueryable();


                // -----------------------------------------------------
                // Date Filters
                // -----------------------------------------------------

                if (fromDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;

                    salesQuery = salesQuery
                        .Where(s => s.SaleDate >= startDate);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);

                    salesQuery = salesQuery
                        .Where(s => s.SaleDate < endDate);
                }


                // -----------------------------------------------------
                // Get Sales Data
                // -----------------------------------------------------

                var sales = await salesQuery
                    .OrderByDescending(s => s.SaleDate)
                    .Select(s => new
                    {
                        saleNumber = s.SaleNumber,

                        date = s.SaleDate,

                        customer = s.Customer != null
                            ? s.Customer.Name
                            : "Walk-in Customer",

                        cashier = s.CashierUser.FullName,

                        subtotal = s.Subtotal,

                        discount = s.DiscountAmount,

                        tax = s.TaxAmount,

                        grandTotal = s.GrandTotal,

                        paymentMethod = s.Payments
                            .Where(p => p.PaymentType == "SalePayment")
                            .Select(p => p.PaymentMethod)
                            .FirstOrDefault(),

                        status = s.Status
                    })
                    .ToListAsync();


                // -----------------------------------------------------
                // Excel Table Headers
                // -----------------------------------------------------

                worksheet.Cells["A4"].Value = "Sale Number";
                worksheet.Cells["B4"].Value = "Date";
                worksheet.Cells["C4"].Value = "Customer";
                worksheet.Cells["D4"].Value = "Cashier";
                worksheet.Cells["E4"].Value = "Subtotal";
                worksheet.Cells["F4"].Value = "Discount";
                worksheet.Cells["G4"].Value = "Tax";
                worksheet.Cells["H4"].Value = "Grand Total";
                worksheet.Cells["I4"].Value = "Payment Method";
                worksheet.Cells["J4"].Value = "Status";


                // -----------------------------------------------------
                // Header Formatting
                // -----------------------------------------------------

                using (var header = worksheet.Cells["A4:J4"])
                {
                    header.Style.Font.Bold = true;
                    header.Style.Fill.PatternType =
                        OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    header.Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.LightGray);
                }


                // -----------------------------------------------------
                // Insert Sales Data
                // -----------------------------------------------------

                var row = 5;

                foreach (var sale in sales)
                {
                    worksheet.Cells[row, 1].Value = sale.saleNumber;
                    worksheet.Cells[row, 2].Value = sale.date;
                    worksheet.Cells[row, 3].Value = sale.customer;
                    worksheet.Cells[row, 4].Value = sale.cashier;
                    worksheet.Cells[row, 5].Value = sale.subtotal;
                    worksheet.Cells[row, 6].Value = sale.discount;
                    worksheet.Cells[row, 7].Value = sale.tax;
                    worksheet.Cells[row, 8].Value = sale.grandTotal;
                    worksheet.Cells[row, 9].Value =
                        sale.paymentMethod ?? "Not Paid";
                    worksheet.Cells[row, 10].Value = sale.status;

                    row++;
                }


                // -----------------------------------------------------
                // Formatting
                // -----------------------------------------------------

                if (sales.Count > 0)
                {
                    worksheet.Column(2)
                        .Style.Numberformat.Format =
                        "dd-MM-yyyy HH:mm";

                    worksheet.Cells[5, 5, row - 1, 8]
                        .Style.Numberformat.Format =
                        "₹#,##0.00";


                    // Excel Table
                    var table =
                        worksheet.Tables.Add(
                            worksheet.Cells[4, 1, row - 1, 10],
                            "SalesTable");

                    table.TableStyle =
                        OfficeOpenXml.Table.TableStyles.Medium2;
                }


                // =====================================================
                // SALES CHART DATA
                // =====================================================

                var salesByDate = sales
                    .GroupBy(s => s.date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(x => x.grandTotal)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();


                // -----------------------------------------------------
                // Chart Data
                // -----------------------------------------------------

                var chartStartRow = row + 3;

                worksheet.Cells[chartStartRow, 1].Value = "Date";
                worksheet.Cells[chartStartRow, 2].Value = "Sales Amount";

                for (int i = 0; i < salesByDate.Count; i++)
                {
                    worksheet.Cells[
                        chartStartRow + i + 1,
                        1
                    ].Value = salesByDate[i].Date;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        2
                    ].Value = salesByDate[i].Total;
                }


                // =====================================================
                // SALES AMOUNT BY DATE - COLUMN CHART
                // =====================================================

                if (salesByDate.Count > 0)
                {
                    var salesChart =
                        worksheet.Drawings.AddChart(
                            "SalesAmountChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);

                    salesChart.Title.Text =
                        "Sales Amount by Date";

                    salesChart.SetPosition(
                        chartStartRow - 1,
                        3,
                        chartStartRow + 16,
                        11);

                    salesChart.SetSize(700, 400);


                    var salesSeries =
                        salesChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                2,
                                chartStartRow + salesByDate.Count,
                                2
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + salesByDate.Count,
                                1
                            ]);

                    salesSeries.Header =
                        "Sales Amount";

                    salesChart.Legend.Remove();
                }


                // =====================================================
                // PAYMENT METHOD DATA
                // =====================================================

                var paymentSummary = sales
                    .Where(s =>
                        !string.IsNullOrWhiteSpace(
                            s.paymentMethod))
                    .GroupBy(s => s.paymentMethod!)
                    .Select(g => new
                    {
                        Method = g.Key,
                        Amount = g.Sum(x => x.grandTotal)
                    })
                    .ToList();


                // -----------------------------------------------------
                // Payment Chart Data
                // -----------------------------------------------------

                var paymentStartRow =
                    chartStartRow + salesByDate.Count + 3;

                worksheet.Cells[paymentStartRow, 1].Value =
                    "Payment Method";

                worksheet.Cells[paymentStartRow, 2].Value =
                    "Amount";


                for (int i = 0; i < paymentSummary.Count; i++)
                {
                    worksheet.Cells[
                        paymentStartRow + i + 1,
                        1
                    ].Value = paymentSummary[i].Method;

                    worksheet.Cells[
                        paymentStartRow + i + 1,
                        2
                    ].Value = paymentSummary[i].Amount;
                }


                // =====================================================
                // PAYMENT METHOD - DOUGHNUT CHART
                // =====================================================

                if (paymentSummary.Count > 0)
                {
                    var paymentChart =
                        worksheet.Drawings.AddChart(
                            "PaymentMethodChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.Doughnut);

                    paymentChart.Title.Text =
                        "Payment Method Distribution";

                    paymentChart.SetPosition(
                        chartStartRow - 1,
                        12,
                        chartStartRow + 16,
                        20);

                    paymentChart.SetSize(500, 400);


                    var paymentSeries =
                        paymentChart.Series.Add(
                            worksheet.Cells[
                                paymentStartRow + 1,
                                2,
                                paymentStartRow + paymentSummary.Count,
                                2
                            ],
                            worksheet.Cells[
                                paymentStartRow + 1,
                                1,
                                paymentStartRow + paymentSummary.Count,
                                1
                            ]);

                    paymentSeries.Header =
                        "Payment Amount";

                    paymentChart.Legend.Position =
                        OfficeOpenXml.Drawing.Chart.eLegendPosition.Right;
                }
            }

            //---------------------------------------------------------
            //Sales Report End
            // --------------------------------------------------------


            // =========================================================
            // PRODUCTS REPORT
            // =========================================================

            if (reportType == "Products")
            {
                var saleItemsQuery = _context.Saleitems
                    .Include(si => si.Product)
                    .Include(si => si.Sale)
                    .Where(si => si.Sale.Status == "Paid");


                // -----------------------------------------------------
                // Date Filters
                // -----------------------------------------------------

                if (fromDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;

                    saleItemsQuery = saleItemsQuery
                        .Where(si => si.Sale.SaleDate >= startDate);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);

                    saleItemsQuery = saleItemsQuery
                        .Where(si => si.Sale.SaleDate < endDate);
                }


                // -----------------------------------------------------
                // Get Product Data
                // -----------------------------------------------------

                var products = await saleItemsQuery
                    .GroupBy(si => new
                    {
                        si.ProductId,
                        si.Product.Name
                    })
                    .Select(g => new
                    {
                        productId = g.Key.ProductId,

                        productName = g.Key.Name,

                        quantitySold = g.Sum(x => x.Quantity),

                        salesAmount = g.Sum(x => x.Subtotal)
                    })
                    .OrderByDescending(x => x.quantitySold)
                    .ToListAsync();


                // -----------------------------------------------------
                // Headers
                // -----------------------------------------------------

                worksheet.Cells["A4"].Value = "Product";
                worksheet.Cells["B4"].Value = "Quantity Sold";
                worksheet.Cells["C4"].Value = "Sales Amount";


                // -----------------------------------------------------
                // Header Formatting
                // -----------------------------------------------------

                using (var header = worksheet.Cells["A4:C4"])
                {
                    header.Style.Font.Bold = true;

                    header.Style.Fill.PatternType =
                        OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    header.Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.LightGray);
                }


                // -----------------------------------------------------
                // Insert Product Data
                // -----------------------------------------------------

                var row = 5;

                foreach (var product in products)
                {
                    worksheet.Cells[row, 1].Value =
                        product.productName;

                    worksheet.Cells[row, 2].Value =
                        product.quantitySold;

                    worksheet.Cells[row, 3].Value =
                        product.salesAmount;

                    row++;
                }


                // -----------------------------------------------------
                // Formatting + Excel Table
                // -----------------------------------------------------

                if (products.Count > 0)
                {
                    worksheet.Cells[5, 3, row - 1, 3]
                        .Style.Numberformat.Format =
                        "₹#,##0.00";


                    var table =
                        worksheet.Tables.Add(
                            worksheet.Cells[4, 1, row - 1, 3],
                            "ProductsTable");

                    table.TableStyle =
                        OfficeOpenXml.Table.TableStyles.Medium2;
                }


                // =====================================================
                // PRODUCT CHART DATA
                // =====================================================

                var chartStartRow = row + 3;

                worksheet.Cells[chartStartRow, 1].Value =
                    "Product";

                worksheet.Cells[chartStartRow, 2].Value =
                    "Quantity Sold";


                for (int i = 0; i < products.Count; i++)
                {
                    worksheet.Cells[
                        chartStartRow + i + 1,
                        1
                    ].Value = products[i].productName;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        2
                    ].Value = products[i].quantitySold;
                }


                // =====================================================
                // TOP PRODUCTS - HORIZONTAL BAR CHART
                // =====================================================

                if (products.Count > 0)
                {
                    var productsChart =
                        worksheet.Drawings.AddChart(
                            "TopProductsChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.BarClustered);

                    productsChart.Title.Text =
                        "Top Products by Quantity Sold";

                    productsChart.SetPosition(
                        chartStartRow - 1,
                        4,
                        chartStartRow + 18,
                        14);

                    productsChart.SetSize(700, 450);


                    var productsSeries =
                        productsChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                2,
                                chartStartRow + products.Count,
                                2
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + products.Count,
                                1
                            ]);

                    productsSeries.Header =
                        "Quantity Sold";

                    productsChart.Legend.Remove();
                }
            }
            //---------------------------------------------------------
            //products report end
            //---------------------------------------------------------

            // =========================================================
            // RETURNS REPORT
            // =========================================================

            if (reportType == "Returns")
            {
                var returnsQuery = _context.Returns
                    .Include(r => r.Sale)
                    .Include(r => r.ProcessedByUser)
                    .Where(r => r.Status == "Completed");


                // -----------------------------------------------------
                // Date Filters
                // -----------------------------------------------------

                if (fromDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;

                    returnsQuery = returnsQuery
                        .Where(r => r.ReturnDate >= startDate);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);

                    returnsQuery = returnsQuery
                        .Where(r => r.ReturnDate < endDate);
                }


                // -----------------------------------------------------
                // Get Returns Data
                // -----------------------------------------------------

                var returns = await returnsQuery
                    .OrderByDescending(r => r.ReturnDate)
                    .Select(r => new
                    {
                        returnNumber = r.ReturnNumber,

                        saleNumber = r.Sale.SaleNumber,

                        returnDate = r.ReturnDate,

                        refundAmount = r.RefundAmount,

                        processedBy = r.ProcessedByUser.FullName,

                        reason = r.Reason
                    })
                    .ToListAsync();


                // -----------------------------------------------------
                // Headers
                // -----------------------------------------------------

                worksheet.Cells["A4"].Value = "Return Number";
                worksheet.Cells["B4"].Value = "Sale Number";
                worksheet.Cells["C4"].Value = "Return Date";
                worksheet.Cells["D4"].Value = "Refund Amount";
                worksheet.Cells["E4"].Value = "Processed By";
                worksheet.Cells["F4"].Value = "Reason";


                // -----------------------------------------------------
                // Header Formatting
                // -----------------------------------------------------

                using (var header = worksheet.Cells["A4:F4"])
                {
                    header.Style.Font.Bold = true;

                    header.Style.Fill.PatternType =
                        OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    header.Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.LightGray);
                }


                // -----------------------------------------------------
                // Insert Return Data
                // -----------------------------------------------------

                var row = 5;

                foreach (var item in returns)
                {
                    worksheet.Cells[row, 1].Value =
                        item.returnNumber;

                    worksheet.Cells[row, 2].Value =
                        item.saleNumber;

                    worksheet.Cells[row, 3].Value =
                        item.returnDate;

                    worksheet.Cells[row, 4].Value =
                        item.refundAmount;

                    worksheet.Cells[row, 5].Value =
                        item.processedBy;

                    worksheet.Cells[row, 6].Value =
                        item.reason ?? "No reason recorded.";

                    row++;
                }


                // -----------------------------------------------------
                // Formatting + Excel Table
                // -----------------------------------------------------

                if (returns.Count > 0)
                {
                    worksheet.Column(3)
                        .Style.Numberformat.Format =
                        "dd-MM-yyyy HH:mm";

                    worksheet.Cells[5, 4, row - 1, 4]
                        .Style.Numberformat.Format =
                        "₹#,##0.00";


                    var table =
                        worksheet.Tables.Add(
                            worksheet.Cells[4, 1, row - 1, 6],
                            "ReturnsTable");

                    table.TableStyle =
                        OfficeOpenXml.Table.TableStyles.Medium2;
                }


                // =====================================================
                // RETURN CHART DATA
                // =====================================================

                var returnsByDate = returns
                    .GroupBy(r => r.returnDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,

                        ReturnCount = g.Count(),

                        RefundAmount =
                            g.Sum(x => x.refundAmount)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();


                // -----------------------------------------------------
                // Chart Data
                // -----------------------------------------------------

                var chartStartRow = row + 3;

                worksheet.Cells[chartStartRow, 1].Value =
                    "Date";

                worksheet.Cells[chartStartRow, 2].Value =
                    "Return Count";

                worksheet.Cells[chartStartRow, 3].Value =
                    "Refund Amount";


                for (int i = 0; i < returnsByDate.Count; i++)
                {
                    worksheet.Cells[
                        chartStartRow + i + 1,
                        1
                    ].Value = returnsByDate[i].Date;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        2
                    ].Value = returnsByDate[i].ReturnCount;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        3
                    ].Value = returnsByDate[i].RefundAmount;
                }


                // =====================================================
                // RETURNS COUNT - COLUMN CHART
                // =====================================================

                if (returnsByDate.Count > 0)
                {
                    var returnsCountChart =
                        worksheet.Drawings.AddChart(
                            "ReturnsCountChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);

                    returnsCountChart.Title.Text =
                        "Returns Count by Date";

                    returnsCountChart.SetPosition(
                        chartStartRow - 1,
                        4,
                        chartStartRow + 16,
                        11);

                    returnsCountChart.SetSize(600, 400);


                    var countSeries =
                        returnsCountChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                2,
                                chartStartRow + returnsByDate.Count,
                                2
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + returnsByDate.Count,
                                1
                            ]);

                    countSeries.Header =
                        "Return Count";

                    returnsCountChart.Legend.Remove();
                }


                // =====================================================
                // REFUND AMOUNT - LINE CHART
                // =====================================================

                if (returnsByDate.Count > 0)
                {
                    var refundChart =
                        worksheet.Drawings.AddChart(
                            "RefundAmountChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.Line);

                    refundChart.Title.Text =
                        "Refund Amount by Date";

                    refundChart.SetPosition(
                        chartStartRow - 1,
                        12,
                        chartStartRow + 16,
                        20);

                    refundChart.SetSize(600, 400);


                    var refundSeries =
                        refundChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                3,
                                chartStartRow + returnsByDate.Count,
                                3
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + returnsByDate.Count,
                                1
                            ]);

                    refundSeries.Header =
                        "Refund Amount";
                }
            }
            //-------------------------------------------------------
            //returns report end
            //-------------------------------------------------------


            // =========================================================
            // CUSTOMERS REPORT
            // =========================================================

            if (reportType == "Customers")
            {
                var salesQuery = _context.Sales
                    .Include(s => s.Customer)
                    .Where(s =>
                        s.Status == "Paid" &&
                        s.CustomerId != null);


                // -----------------------------------------------------
                // Date Filters
                // -----------------------------------------------------

                if (fromDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;

                    salesQuery = salesQuery
                        .Where(s => s.SaleDate >= startDate);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);

                    salesQuery = salesQuery
                        .Where(s => s.SaleDate < endDate);
                }


                // -----------------------------------------------------
                // Get Customer Data
                // -----------------------------------------------------

                var customers = await salesQuery
                    .GroupBy(s => new
                    {
                        s.CustomerId,
                        CustomerName = s.Customer!.Name
                    })
                    .Select(g => new
                    {
                        customerId = g.Key.CustomerId,

                        customerName = g.Key.CustomerName,

                        salesCount = g.Count(),

                        totalSpent = g.Sum(s => s.GrandTotal),

                        averageOrderValue =
                            g.Average(s => s.GrandTotal)
                    })
                    .OrderByDescending(x => x.totalSpent)
                    .ToListAsync();


                // -----------------------------------------------------
                // Headers
                // -----------------------------------------------------

                worksheet.Cells["A4"].Value = "Customer";
                worksheet.Cells["B4"].Value = "Sales";
                worksheet.Cells["C4"].Value = "Total Spent";
                worksheet.Cells["D4"].Value = "Average Order";


                // -----------------------------------------------------
                // Header Formatting
                // -----------------------------------------------------

                using (var header = worksheet.Cells["A4:D4"])
                {
                    header.Style.Font.Bold = true;

                    header.Style.Fill.PatternType =
                        OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    header.Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.LightGray);
                }


                // -----------------------------------------------------
                // Insert Customer Data
                // -----------------------------------------------------

                var row = 5;

                foreach (var customer in customers)
                {
                    worksheet.Cells[row, 1].Value =
                        customer.customerName;

                    worksheet.Cells[row, 2].Value =
                        customer.salesCount;

                    worksheet.Cells[row, 3].Value =
                        customer.totalSpent;

                    worksheet.Cells[row, 4].Value =
                        customer.averageOrderValue;

                    row++;
                }


                // -----------------------------------------------------
                // Formatting + Excel Table
                // -----------------------------------------------------

                if (customers.Count > 0)
                {
                    worksheet.Cells[5, 3, row - 1, 4]
                        .Style.Numberformat.Format =
                        "₹#,##0.00";


                    var table =
                        worksheet.Tables.Add(
                            worksheet.Cells[4, 1, row - 1, 4],
                            "CustomersTable");

                    table.TableStyle =
                        OfficeOpenXml.Table.TableStyles.Medium2;
                }


                // =====================================================
                // CUSTOMER CHART DATA
                // =====================================================

                var chartStartRow = row + 3;

                worksheet.Cells[chartStartRow, 1].Value =
                    "Customer";

                worksheet.Cells[chartStartRow, 2].Value =
                    "Total Spent";

                worksheet.Cells[chartStartRow, 3].Value =
                    "Number of Purchases";


                for (int i = 0; i < customers.Count; i++)
                {
                    worksheet.Cells[
                        chartStartRow + i + 1,
                        1
                    ].Value = customers[i].customerName;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        2
                    ].Value = customers[i].totalSpent;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        3
                    ].Value = customers[i].salesCount;
                }


                // =====================================================
                // TOP CUSTOMERS - TOTAL SPENDING
                // =====================================================

                if (customers.Count > 0)
                {
                    var spendingChart =
                        worksheet.Drawings.AddChart(
                            "CustomerSpendingChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.BarClustered);

                    spendingChart.Title.Text =
                        "Top Customers by Total Spending";

                    spendingChart.SetPosition(
                        chartStartRow - 1,
                        4,
                        chartStartRow + 18,
                        13);

                    spendingChart.SetSize(650, 450);


                    var spendingSeries =
                        spendingChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                2,
                                chartStartRow + customers.Count,
                                2
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + customers.Count,
                                1
                            ]);

                    spendingSeries.Header =
                        "Total Spent";

                    spendingChart.Legend.Remove();
                }


                // =====================================================
                // CUSTOMERS - NUMBER OF PURCHASES
                // =====================================================

                if (customers.Count > 0)
                {
                    var purchasesChart =
                        worksheet.Drawings.AddChart(
                            "CustomerPurchasesChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.BarClustered);

                    purchasesChart.Title.Text =
                        "Customers by Number of Purchases";

                    purchasesChart.SetPosition(
                        chartStartRow - 1,
                        14,
                        chartStartRow + 18,
                        23);

                    purchasesChart.SetSize(650, 450);


                    var purchasesSeries =
                        purchasesChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                3,
                                chartStartRow + customers.Count,
                                3
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + customers.Count,
                                1
                            ]);

                    purchasesSeries.Header =
                        "Number of Purchases";

                    purchasesChart.Legend.Remove();
                }
            }

            //=========================================================
            //customers report end
            //=========================================================

            // =========================================================
            // INCOME REPORT
            // =========================================================

            if (reportType == "Income")
            {
                var salesQuery = _context.Sales
                    .Where(s => s.Status == "Paid");


                // -----------------------------------------------------
                // Date Filters
                // -----------------------------------------------------

                if (fromDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;

                    salesQuery = salesQuery
                        .Where(s => s.SaleDate >= startDate);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);

                    salesQuery = salesQuery
                        .Where(s => s.SaleDate < endDate);
                }


                // -----------------------------------------------------
                // Get Income Data
                // -----------------------------------------------------

                var income = await salesQuery
                    .OrderBy(s => s.SaleDate)
                    .Select(s => new
                    {
                        saleNumber = s.SaleNumber,

                        saleDate = s.SaleDate,

                        subtotal = s.Subtotal,

                        discount = s.DiscountAmount,

                        tax = s.TaxAmount,

                        grandTotal = s.GrandTotal
                    })
                    .ToListAsync();


                // -----------------------------------------------------
                // Headers
                // -----------------------------------------------------

                worksheet.Cells["A4"].Value = "Sale Number";
                worksheet.Cells["B4"].Value = "Sale Date";
                worksheet.Cells["C4"].Value = "Subtotal";
                worksheet.Cells["D4"].Value = "Discount";
                worksheet.Cells["E4"].Value = "Tax";
                worksheet.Cells["F4"].Value = "Grand Total";


                // -----------------------------------------------------
                // Header Formatting
                // -----------------------------------------------------

                using (var header = worksheet.Cells["A4:F4"])
                {
                    header.Style.Font.Bold = true;

                    header.Style.Fill.PatternType =
                        OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    header.Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.LightGray);
                }


                // -----------------------------------------------------
                // Insert Income Data
                // -----------------------------------------------------

                var row = 5;

                foreach (var sale in income)
                {
                    worksheet.Cells[row, 1].Value =
                        sale.saleNumber;

                    worksheet.Cells[row, 2].Value =
                        sale.saleDate;

                    worksheet.Cells[row, 3].Value =
                        sale.subtotal;

                    worksheet.Cells[row, 4].Value =
                        sale.discount;

                    worksheet.Cells[row, 5].Value =
                        sale.tax;

                    worksheet.Cells[row, 6].Value =
                        sale.grandTotal;

                    row++;
                }


                // -----------------------------------------------------
                // Formatting + Excel Table
                // -----------------------------------------------------

                if (income.Count > 0)
                {
                    worksheet.Column(2)
                        .Style.Numberformat.Format =
                        "dd-MM-yyyy HH:mm";

                    worksheet.Cells[5, 3, row - 1, 6]
                        .Style.Numberformat.Format =
                        "₹#,##0.00";


                    var table =
                        worksheet.Tables.Add(
                            worksheet.Cells[4, 1, row - 1, 6],
                            "IncomeTable");

                    table.TableStyle =
                        OfficeOpenXml.Table.TableStyles.Medium2;
                }


                // =====================================================
                // INCOME CHART DATA
                // =====================================================

                var incomeByDate = income
                    .GroupBy(x => x.saleDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,

                        Subtotal =
                            g.Sum(x => x.subtotal),

                        Discount =
                            g.Sum(x => x.discount),

                        Tax =
                            g.Sum(x => x.tax),

                        GrandTotal =
                            g.Sum(x => x.grandTotal)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();


                var chartStartRow = row + 3;

                worksheet.Cells[chartStartRow, 1].Value =
                    "Date";

                worksheet.Cells[chartStartRow, 2].Value =
                    "Subtotal";

                worksheet.Cells[chartStartRow, 3].Value =
                    "Discount";

                worksheet.Cells[chartStartRow, 4].Value =
                    "Tax";

                worksheet.Cells[chartStartRow, 5].Value =
                    "Grand Total";


                for (int i = 0; i < incomeByDate.Count; i++)
                {
                    worksheet.Cells[
                        chartStartRow + i + 1,
                        1
                    ].Value = incomeByDate[i].Date;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        2
                    ].Value = incomeByDate[i].Subtotal;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        3
                    ].Value = incomeByDate[i].Discount;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        4
                    ].Value = incomeByDate[i].Tax;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        5
                    ].Value = incomeByDate[i].GrandTotal;
                }


                // =====================================================
                // INCOME TREND - LINE CHART
                // =====================================================

                if (incomeByDate.Count > 0)
                {
                    var incomeChart =
                        worksheet.Drawings.AddChart(
                            "IncomeTrendChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.Line);

                    incomeChart.Title.Text =
                        "Income Trend";

                    incomeChart.SetPosition(
                        chartStartRow - 1,
                        6,
                        chartStartRow + 18,
                        14);

                    incomeChart.SetSize(650, 450);


                    var incomeSeries =
                        incomeChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                5,
                                chartStartRow + incomeByDate.Count,
                                5
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + incomeByDate.Count,
                                1
                            ]);

                    incomeSeries.Header =
                        "Grand Total";

                    incomeChart.Legend.Remove();
                }


                // =====================================================
                // FINANCIAL COMPONENTS - MULTI-LINE CHART
                // =====================================================

                if (incomeByDate.Count > 0)
                {
                    var breakdownChart =
                        worksheet.Drawings.AddChart(
                            "FinancialComponentsChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.Line);

                    breakdownChart.Title.Text =
                        "Financial Components";

                    breakdownChart.SetPosition(
                        chartStartRow - 1,
                        15,
                        chartStartRow + 18,
                        25);

                    breakdownChart.SetSize(750, 450);


                    var subtotalSeries =
                        breakdownChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                2,
                                chartStartRow + incomeByDate.Count,
                                2
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + incomeByDate.Count,
                                1
                            ]);

                    subtotalSeries.Header =
                        "Subtotal";


                    var discountSeries =
                        breakdownChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                3,
                                chartStartRow + incomeByDate.Count,
                                3
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + incomeByDate.Count,
                                1
                            ]);

                    discountSeries.Header =
                        "Discount";


                    var taxSeries =
                        breakdownChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                4,
                                chartStartRow + incomeByDate.Count,
                                4
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + incomeByDate.Count,
                                1
                            ]);

                    taxSeries.Header =
                        "Tax";


                    var grandTotalSeries =
                        breakdownChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                5,
                                chartStartRow + incomeByDate.Count,
                                5
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + incomeByDate.Count,
                                1
                            ]);

                    grandTotalSeries.Header =
                        "Grand Total";


                    breakdownChart.Legend.Position =
                        OfficeOpenXml.Drawing.Chart.eLegendPosition.Bottom;
                }
            }
            //=========================================================
            //income report end
            //=========================================================


            // =========================================================
            // INVENTORY REPORT
            // =========================================================

            if (reportType == "Inventory")
            {
                var products = await _context.Products
                    .Where(p => p.IsActive == true)
                    .Select(p => new
                    {
                        productId = p.ProductId,

                        productName = p.Name,

                        sku = p.Sku,

                        currentStock = p.CurrentStock,

                        reorderLevel = p.ReorderLevel,

                        status = p.CurrentStock == 0
                            ? "Out of Stock"
                            : p.CurrentStock <= p.ReorderLevel
                                ? "Low Stock"
                                : "In Stock"
                    })
                    .OrderBy(p => p.currentStock)
                    .ToListAsync();


                // -----------------------------------------------------
                // Headers
                // -----------------------------------------------------

                worksheet.Cells["A4"].Value = "Product";
                worksheet.Cells["B4"].Value = "SKU";
                worksheet.Cells["C4"].Value = "Current Stock";
                worksheet.Cells["D4"].Value = "Reorder Level";
                worksheet.Cells["E4"].Value = "Status";


                // -----------------------------------------------------
                // Header Formatting
                // -----------------------------------------------------

                using (var header = worksheet.Cells["A4:E4"])
                {
                    header.Style.Font.Bold = true;

                    header.Style.Fill.PatternType =
                        OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    header.Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.LightGray);
                }


                // -----------------------------------------------------
                // Insert Inventory Data
                // -----------------------------------------------------

                var row = 5;

                foreach (var product in products)
                {
                    worksheet.Cells[row, 1].Value =
                        product.productName;

                    worksheet.Cells[row, 2].Value =
                        product.sku;

                    worksheet.Cells[row, 3].Value =
                        product.currentStock;

                    worksheet.Cells[row, 4].Value =
                        product.reorderLevel;

                    worksheet.Cells[row, 5].Value =
                        product.status;

                    row++;
                }


                // -----------------------------------------------------
                // Excel Table
                // -----------------------------------------------------

                if (products.Count > 0)
                {
                    var table =
                        worksheet.Tables.Add(
                            worksheet.Cells[4, 1, row - 1, 5],
                            "InventoryTable");

                    table.TableStyle =
                        OfficeOpenXml.Table.TableStyles.Medium2;
                }


                // =====================================================
                // INVENTORY CHART DATA
                // =====================================================

                var chartStartRow = row + 3;

                worksheet.Cells[chartStartRow, 1].Value =
                    "Product";

                worksheet.Cells[chartStartRow, 2].Value =
                    "Current Stock";


                for (int i = 0; i < products.Count; i++)
                {
                    worksheet.Cells[
                        chartStartRow + i + 1,
                        1
                    ].Value = products[i].productName;

                    worksheet.Cells[
                        chartStartRow + i + 1,
                        2
                    ].Value = products[i].currentStock;
                }


                // =====================================================
                // CURRENT STOCK - HORIZONTAL BAR CHART
                // =====================================================

                if (products.Count > 0)
                {
                    var stockChart =
                        worksheet.Drawings.AddChart(
                            "CurrentStockChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.BarClustered);

                    stockChart.Title.Text =
                        "Current Stock by Product";

                    stockChart.SetPosition(
                        chartStartRow - 1,
                        4,
                        chartStartRow + 20,
                        14);

                    stockChart.SetSize(700, 500);


                    var stockSeries =
                        stockChart.Series.Add(
                            worksheet.Cells[
                                chartStartRow + 1,
                                2,
                                chartStartRow + products.Count,
                                2
                            ],
                            worksheet.Cells[
                                chartStartRow + 1,
                                1,
                                chartStartRow + products.Count,
                                1
                            ]);

                    stockSeries.Header =
                        "Current Stock";

                    stockChart.Legend.Remove();
                }


                // =====================================================
                // INVENTORY STATUS SUMMARY
                // =====================================================

                var statusSummary = products
                    .GroupBy(p => p.status)
                    .Select(g => new
                    {
                        Status = g.Key,

                        Count = g.Count()
                    })
                    .ToList();


                var statusStartRow =
                    chartStartRow + products.Count + 3;


                worksheet.Cells[statusStartRow, 1].Value =
                    "Status";

                worksheet.Cells[statusStartRow, 2].Value =
                    "Product Count";


                for (int i = 0; i < statusSummary.Count; i++)
                {
                    worksheet.Cells[
                        statusStartRow + i + 1,
                        1
                    ].Value = statusSummary[i].Status;

                    worksheet.Cells[
                        statusStartRow + i + 1,
                        2
                    ].Value = statusSummary[i].Count;
                }


                // =====================================================
                // INVENTORY STATUS - DOUGHNUT CHART
                // =====================================================

                if (statusSummary.Count > 0)
                {
                    var statusChart =
                        worksheet.Drawings.AddChart(
                            "InventoryStatusChart",
                            OfficeOpenXml.Drawing.Chart.eChartType.Doughnut);

                    statusChart.Title.Text =
                        "Inventory Status Distribution";

                    statusChart.SetPosition(
                        chartStartRow - 1,
                        15,
                        chartStartRow + 20,
                        25);

                    statusChart.SetSize(550, 500);


                    var statusSeries =
                        statusChart.Series.Add(
                            worksheet.Cells[
                                statusStartRow + 1,
                                2,
                                statusStartRow + statusSummary.Count,
                                2
                            ],
                            worksheet.Cells[
                                statusStartRow + 1,
                                1,
                                statusStartRow + statusSummary.Count,
                                1
                            ]);

                    statusSeries.Header =
                        "Product Count";

                    statusChart.Legend.Position =
                        OfficeOpenXml.Drawing.Chart.eLegendPosition.Right;
                }
            }

            //=========================================================
            //inventory report end
            //=========================================================


            // ---------------------------------------------------------
            // Auto Fit Columns
            // ---------------------------------------------------------
            if (worksheet.Dimension != null)
            {
                worksheet.Cells[worksheet.Dimension.Address]
                    .AutoFitColumns();

                worksheet.Column(1).Width = 25;
                worksheet.Column(2).Width = 18;
                worksheet.Column(3).Width = 18;
                worksheet.Column(4).Width = 18;
                worksheet.Column(5).Width = 20;
                worksheet.Column(6).Width = 25;
            }


            // ---------------------------------------------------------
            // Generate Excel File
            // ---------------------------------------------------------

            var fileBytes =
                package.GetAsByteArray();

            var fileName =
                $"{reportType}_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx";


            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


    }
}