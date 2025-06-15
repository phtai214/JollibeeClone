using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Thống kê doanh thu";
            return View();
        }

        [HttpPost]
        public IActionResult ExportReport(string dateRange)
        {
            // Tạo fake data cho báo cáo Excel
            var reportData = GenerateFakeReportData(dateRange);

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
            worksheet.Cell(4, 1).Value = "TỔNG QUAN";
            worksheet.Cell(4, 1).Style.Font.Bold = true;
            worksheet.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Cell(5, 1).Value = "Tổng doanh thu:";
            worksheet.Cell(5, 2).Value = "70,000,000 VNĐ";
            worksheet.Cell(6, 1).Value = "Đã thanh toán:";
            worksheet.Cell(6, 2).Value = "65,000,000 VNĐ";
            worksheet.Cell(7, 1).Value = "Tổng đơn hàng:";
            worksheet.Cell(7, 2).Value = "120";
            worksheet.Cell(8, 1).Value = "Đơn đã thanh toán:";
            worksheet.Cell(8, 2).Value = "108";

            // Chi tiết theo ngày
            worksheet.Cell(10, 1).Value = "CHI TIẾT THEO NGÀY";
            worksheet.Cell(10, 1).Style.Font.Bold = true;
            worksheet.Cell(10, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            // Headers
            var headers = new[] { "Ngày", "Doanh thu", "Số đơn", "Sản phẩm bán chạy", "Đã bán" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(11, i + 1).Value = headers[i];
                worksheet.Cell(11, i + 1).Style.Font.Bold = true;
                worksheet.Cell(11, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data
            for (int i = 0; i < reportData.Count; i++)
            {
                var data = reportData[i];
                worksheet.Cell(12 + i, 1).Value = data.Date;
                worksheet.Cell(12 + i, 2).Value = data.Revenue;
                worksheet.Cell(12 + i, 3).Value = data.OrderCount;
                worksheet.Cell(12 + i, 4).Value = data.BestSellingProduct;
                worksheet.Cell(12 + i, 5).Value = data.ProductSold;
            }

            // Auto-fit columns
            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BaoCao_DoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private List<ReportData> GenerateFakeReportData(string dateRange)
        {
            var data = new List<ReportData>();
            var products = new[] { "Burger Tôm", "Gà Rán", "Gà Quay", "Mì Ý", "Burger Bò" };
            var random = new Random();

            for (int i = 1; i <= 30; i++)
            {
                data.Add(new ReportData
                {
                    Date = DateTime.Now.AddDays(-i).ToString("dd/MM/yyyy"),
                    Revenue = $"{random.Next(1500000, 3000000):N0} VNĐ",
                    OrderCount = random.Next(5, 15),
                    BestSellingProduct = products[random.Next(products.Length)],
                    ProductSold = random.Next(20, 50)
                });
            }

            return data;
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
    }
} 