using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.Areas.Admin.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class StatisticsController : Controller
    {
        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Thống kê doanh thu";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatisticsOverview(string dateRange = "week")
        {
            var dateFilter = GetDateRange(dateRange);
            var startDate = dateFilter.StartDate;
            var endDate = dateFilter.EndDate;

            // Lấy thống kê tổng quan
            // Tổng doanh thu: Tất cả đơn hàng (bao gồm cả chưa hoàn thành)
            var totalRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .SumAsync(o => o.TotalAmount);

            // Đã thanh toán: Chỉ tính các đơn hàng đã hoàn thành thành công (OrderStatusID = 6)
            var paidRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate 
                    && o.OrderStatusID == 6) // Hoàn thành = đã thanh toán thành công
                .SumAsync(o => o.TotalAmount);

            // Tổng đơn hàng: Tất cả đơn hàng
            var totalOrders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .CountAsync();

            // Đơn đã thanh toán: Chỉ tính các đơn hàng đã hoàn thành thành công (OrderStatusID = 6)
            var paidOrders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate 
                    && o.OrderStatusID == 6) // Hoàn thành = đã thanh toán thành công
                .CountAsync();

            // Đơn hàng đã hủy: Chỉ tính các đơn hàng có trạng thái "Đã hủy" (OrderStatusID = 7)
            var cancelledOrders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate 
                    && o.OrderStatusID == 7) // Đã hủy
                .CountAsync();

            // Tính phần trăm thay đổi so với kỳ trước
            var previousPeriod = GetDateRange(dateRange, true);
            var prevTotalRevenue = await _context.Orders
                .Where(o => o.OrderDate >= previousPeriod.StartDate && o.OrderDate <= previousPeriod.EndDate)
                .SumAsync(o => o.TotalAmount);

            var prevPaidRevenue = await _context.Orders
                .Where(o => o.OrderDate >= previousPeriod.StartDate && o.OrderDate <= previousPeriod.EndDate
                    && o.OrderStatusID == 6) // Hoàn thành = đã thanh toán thành công
                .SumAsync(o => o.TotalAmount);

            var prevTotalOrders = await _context.Orders
                .Where(o => o.OrderDate >= previousPeriod.StartDate && o.OrderDate <= previousPeriod.EndDate)
                .CountAsync();

            var prevPaidOrders = await _context.Orders
                .Where(o => o.OrderDate >= previousPeriod.StartDate && o.OrderDate <= previousPeriod.EndDate
                    && o.OrderStatusID == 6) // Hoàn thành = đã thanh toán thành công
                .CountAsync();

            var prevCancelledOrders = await _context.Orders
                .Where(o => o.OrderDate >= previousPeriod.StartDate && o.OrderDate <= previousPeriod.EndDate
                    && o.OrderStatusID == 7) // Đã hủy
                .CountAsync();

            var revenueChange = prevTotalRevenue > 0 ? ((totalRevenue - prevTotalRevenue) / prevTotalRevenue * 100) : 0;
            var paidRevenueChange = prevPaidRevenue > 0 ? ((paidRevenue - prevPaidRevenue) / prevPaidRevenue * 100) : 0;
            var ordersChange = prevTotalOrders > 0 ? ((totalOrders - prevTotalOrders) / (decimal)prevTotalOrders * 100) : 0;
            var paidOrdersChange = prevPaidOrders > 0 ? ((paidOrders - prevPaidOrders) / (decimal)prevPaidOrders * 100) : 0;
            var cancelledOrdersChange = prevCancelledOrders > 0 ? ((cancelledOrders - prevCancelledOrders) / (decimal)prevCancelledOrders * 100) : 
                                      (cancelledOrders > 0 ? 100 : 0); // Nếu không có đơn hủy trước đó nhưng có bây giờ thì tăng 100%

            var result = new
            {
                totalRevenue = totalRevenue.ToString("N0"),
                paidRevenue = paidRevenue.ToString("N0"),
                totalOrders = totalOrders,
                paidOrders = paidOrders,
                cancelledOrders = cancelledOrders,
                revenueChange = Math.Round(revenueChange, 1),
                paidRevenueChange = Math.Round(paidRevenueChange, 1),
                ordersChange = Math.Round(ordersChange, 1),
                paidOrdersChange = Math.Round(paidOrdersChange, 1),
                cancelledOrdersChange = Math.Round(cancelledOrdersChange, 1)
            };

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueChartData(string dateRange = "week")
        {
            var dateFilter = GetDateRange(dateRange);
            var startDate = dateFilter.StartDate;
            var endDate = dateFilter.EndDate;

            // Group by date và tính tổng doanh thu theo ngày
            var revenueByDate = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Lấy sản phẩm bán chạy cho từng ngày (tối ưu query)
            var bestSellingByDateQuery = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= startDate && oi.Order.OrderDate <= endDate)
                .GroupBy(oi => new { Date = oi.Order.OrderDate.Date, ProductName = oi.ProductNameSnapshot })
                .Select(g => new { 
                    Date = g.Key.Date, 
                    ProductName = g.Key.ProductName, 
                    TotalSold = g.Sum(oi => oi.Quantity) 
                })
                .ToListAsync();

            var bestProductsByDate = bestSellingByDateQuery
                .GroupBy(x => x.Date)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.TotalSold).First());

            var bestSellingByDate = revenueByDate.Select(item => new
            {
                date = item.Date.ToString("dd/MM"),
                revenue = (double)item.Revenue,
                orderCount = item.OrderCount,
                bestProduct = bestProductsByDate.ContainsKey(item.Date) 
                    ? bestProductsByDate[item.Date].ProductName 
                    : "Không có",
                bestProductSold = bestProductsByDate.ContainsKey(item.Date) 
                    ? bestProductsByDate[item.Date].TotalSold 
                    : 0
            }).ToList();

            var labels = bestSellingByDate.Select(x => x.date).ToList();
            var revenueData = bestSellingByDate.Select(x => x.revenue).ToList();
            var orderData = bestSellingByDate.Select(x => x.orderCount).ToList();

            return Json(new
            {
                labels = labels,
                revenueData = revenueData,
                orderData = orderData,
                bestSellingProducts = bestSellingByDate
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatusData(string dateRange = "week")
        {
            var dateFilter = GetDateRange(dateRange);
            var startDate = dateFilter.StartDate;
            var endDate = dateFilter.EndDate;

            var orderStatusData = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .Include(o => o.OrderStatus)
                .GroupBy(o => o.OrderStatus.StatusName)
                .Select(g => new
                {
                    StatusName = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = orderStatusData.Select(x => x.StatusName).ToList();
            var data = orderStatusData.Select(x => x.Count).ToList();

            return Json(new
            {
                labels = labels,
                data = data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetBestSellingProducts(string dateRange = "week")
        {
            var dateFilter = GetDateRange(dateRange);
            var startDate = dateFilter.StartDate;
            var endDate = dateFilter.EndDate;

            var bestSellingProducts = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= startDate && oi.Order.OrderDate <= endDate)
                .GroupBy(oi => new { oi.ProductID, oi.ProductNameSnapshot })
                .Select(g => new
                {
                    ProductName = g.Key.ProductNameSnapshot,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Subtotal)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(15)
                .ToListAsync();

            return Json(bestSellingProducts);
        }

        [HttpPost]
        public async Task<IActionResult> ExportReport(string dateRange)
        {
            var dateFilter = GetDateRange(dateRange);
            var reportData = await GenerateRealReportData(dateFilter.StartDate, dateFilter.EndDate);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Báo cáo doanh thu");

            // Header
            worksheet.Cell(1, 1).Value = "BÁO CÁO DOANH THU JOLLIBEE";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 6).Merge();

            worksheet.Cell(2, 1).Value = $"Thời gian: {GetDateRangeText(dateRange)}";
            worksheet.Cell(2, 1).Style.Font.FontSize = 12;
            worksheet.Range(2, 1, 2, 6).Merge();

            // Tổng quan
            var totalRevenue = await _context.Orders
                .Where(o => o.OrderDate >= dateFilter.StartDate && o.OrderDate <= dateFilter.EndDate)
                .SumAsync(o => o.TotalAmount);

            var paidRevenue = await _context.Orders
                .Where(o => o.OrderDate >= dateFilter.StartDate && o.OrderDate <= dateFilter.EndDate 
                    && o.OrderStatusID == 6) // Hoàn thành = đã thanh toán thành công
                .SumAsync(o => o.TotalAmount);

            var totalOrders = await _context.Orders
                .Where(o => o.OrderDate >= dateFilter.StartDate && o.OrderDate <= dateFilter.EndDate)
                .CountAsync();

            var paidOrders = await _context.Orders
                .Where(o => o.OrderDate >= dateFilter.StartDate && o.OrderDate <= dateFilter.EndDate 
                    && o.OrderStatusID == 6) // Hoàn thành = đã thanh toán thành công
                .CountAsync();

            var cancelledOrders = await _context.Orders
                .Where(o => o.OrderDate >= dateFilter.StartDate && o.OrderDate <= dateFilter.EndDate 
                    && o.OrderStatusID == 7) // Đã hủy
                .CountAsync();

            worksheet.Cell(4, 1).Value = "TỔNG QUAN";
            worksheet.Cell(4, 1).Style.Font.Bold = true;
            worksheet.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Cell(5, 1).Value = "Tổng doanh thu:";
            worksheet.Cell(5, 2).Value = $"{totalRevenue:N0} VNĐ";
            worksheet.Cell(6, 1).Value = "Đã thanh toán:";
            worksheet.Cell(6, 2).Value = $"{paidRevenue:N0} VNĐ";
            worksheet.Cell(7, 1).Value = "Tổng đơn hàng:";
            worksheet.Cell(7, 2).Value = totalOrders;
            worksheet.Cell(8, 1).Value = "Đơn đã thanh toán:";
            worksheet.Cell(8, 2).Value = paidOrders;
            worksheet.Cell(9, 1).Value = "Đơn hàng đã hủy:";
            worksheet.Cell(9, 2).Value = cancelledOrders;

            // Chi tiết theo ngày
            worksheet.Cell(11, 1).Value = "CHI TIẾT THEO NGÀY";
            worksheet.Cell(11, 1).Style.Font.Bold = true;
            worksheet.Cell(11, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            // Headers
            var headers = new[] { "Ngày", "Doanh thu", "Số đơn", "Sản phẩm bán chạy", "Đã bán" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(12, i + 1).Value = headers[i];
                worksheet.Cell(12, i + 1).Style.Font.Bold = true;
                worksheet.Cell(12, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data
            for (int i = 0; i < reportData.Count; i++)
            {
                var data = reportData[i];
                worksheet.Cell(13 + i, 1).Value = data.Date;
                worksheet.Cell(13 + i, 2).Value = data.Revenue;
                worksheet.Cell(13 + i, 3).Value = data.OrderCount;
                worksheet.Cell(13 + i, 4).Value = data.BestSellingProduct;
                worksheet.Cell(13 + i, 5).Value = data.ProductSold;
            }

            // Auto-fit columns
            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BaoCao_DoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task<List<ReportData>> GenerateRealReportData(DateTime startDate, DateTime endDate)
        {
            var revenueByDate = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var result = new List<ReportData>();

            foreach (var item in revenueByDate)
            {
                // Lấy sản phẩm bán chạy nhất trong ngày
                var bestSellingProduct = await _context.OrderItems
                    .Where(oi => oi.Order.OrderDate.Date == item.Date)
                    .GroupBy(oi => oi.ProductNameSnapshot)
                    .Select(g => new { ProductName = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
                    .OrderByDescending(x => x.TotalSold)
                    .FirstOrDefaultAsync();

                result.Add(new ReportData
                {
                    Date = item.Date.ToString("dd/MM/yyyy"),
                    Revenue = $"{item.Revenue:N0} VNĐ",
                    OrderCount = item.OrderCount,
                    BestSellingProduct = bestSellingProduct?.ProductName ?? "Không có",
                    ProductSold = bestSellingProduct?.TotalSold ?? 0
                });
            }

            return result;
        }

        private (DateTime StartDate, DateTime EndDate) GetDateRange(string dateRange, bool previousPeriod = false)
        {
            var now = DateTime.Now;
            var startDate = now.Date;
            var endDate = now.Date.AddDays(1).AddTicks(-1);

            switch (dateRange.ToLower())
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddDays(-1);
                        endDate = endDate.AddDays(-1);
                    }
                    break;
                case "week":
                    startDate = now.Date.AddDays(-6);
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddDays(-7);
                        endDate = endDate.AddDays(-7);
                    }
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddMonths(-1);
                        endDate = endDate.AddMonths(-1);
                    }
                    break;
                case "quarter":
                    var quarter = (now.Month - 1) / 3 + 1;
                    startDate = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    endDate = startDate.AddMonths(3).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddMonths(-3);
                        endDate = endDate.AddMonths(-3);
                    }
                    break;
                case "year":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = startDate.AddYears(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddYears(-1);
                        endDate = endDate.AddYears(-1);
                    }
                    break;
            }

            return (startDate, endDate);
        }

        private string GetDateRangeText(string dateRange)
        {
            return dateRange switch
            {
                "today" => "Hôm nay",
                "week" => "7 ngày qua",
                "month" => "Tháng này",
                "quarter" => "Quý này",
                "year" => "Năm nay",
                _ => "Tùy chọn"
            };
        }

        private class ReportData
        {
            public string Date { get; set; } = "";
            public string Revenue { get; set; } = "";
            public int OrderCount { get; set; }
            public string BestSellingProduct { get; set; } = "";
            public int ProductSold { get; set; }
        }

        // Test action to create sample cancelled orders
        [HttpPost]
        public async Task<IActionResult> CreateTestCancelledOrders()
        {
            try
            {
                // Check if we already have cancelled orders
                var existingCancelledOrders = await _context.Orders
                    .Where(o => o.OrderStatusID == 7)
                    .CountAsync();

                if (existingCancelledOrders > 0)
                {
                    return Json(new { success = true, message = $"Already have {existingCancelledOrders} cancelled orders" });
                }

                // Create some test cancelled orders for the last 7 days
                var testOrders = new List<Orders>();
                for (int i = 1; i <= 5; i++)
                {
                    var order = new Orders
                    {
                        OrderCode = $"CANCEL-{DateTime.Now:yyyyMMdd}-{i:D3}",
                        CustomerFullName = $"Test Cancelled User {i}",
                        CustomerEmail = $"testcancel{i}@test.com",
                        CustomerPhoneNumber = $"012345678{i}",
                        OrderDate = DateTime.Now.AddDays(-i),
                        SubtotalAmount = 100000 + (i * 50000),
                        ShippingFee = 20000,
                        DiscountAmount = 0,
                        TotalAmount = 120000 + (i * 50000),
                        OrderStatusID = 7, // Đã hủy
                        PaymentMethodID = 1, // Tiền mặt
                        DeliveryMethodID = 1, // Giao hàng
                        NotesByCustomer = "Test order - will be cancelled"
                    };
                    testOrders.Add(order);
                }

                _context.Orders.AddRange(testOrders);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Created {testOrders.Count} test cancelled orders" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Debug action to check current data
        [HttpGet]
        public async Task<IActionResult> CheckCurrentData()
        {
            try
            {
                var totalOrders = await _context.Orders.CountAsync();
                var cancelledOrders = await _context.Orders.Where(o => o.OrderStatusID == 7).CountAsync();
                var completedOrders = await _context.Orders.Where(o => o.OrderStatusID == 6).CountAsync();
                
                var statusBreakdown = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .GroupBy(o => o.OrderStatus.StatusName)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                return Json(new { 
                    success = true, 
                    totalOrders,
                    cancelledOrders,
                    completedOrders,
                    statusBreakdown
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
} 