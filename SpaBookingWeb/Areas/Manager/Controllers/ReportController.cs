using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SpaBookingWeb.Data;
using SpaBookingWeb.ViewModels.Manager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpaBookingWeb.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Manager/Report
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);

            // 1. Lấy dữ liệu Tháng Này từ bảng Transactions
            // Doanh thu: Là các giao dịch Thu (IsIncome = true)
            var currentMonthTransactions = await _context.Transactions
                .Where(t => t.Date >= startOfMonth && t.Date < startOfNextMonth)
                .ToListAsync();

            decimal currentRevenue = currentMonthTransactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal currentExpenses = currentMonthTransactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            int currentOrders = currentMonthTransactions.Count(t => t.IsIncome); // Tạm tính số giao dịch thu là số đơn

            // 2. Lấy dữ liệu Tháng Trước (để so sánh)
            var lastMonthTransactions = await _context.Transactions
                .Where(t => t.Date >= startOfLastMonth && t.Date <= endOfLastMonth && t.IsIncome)
                .ToListAsync();

            decimal lastMonthRevenue = lastMonthTransactions.Sum(t => t.Amount);
            int lastMonthOrders = lastMonthTransactions.Count;

            // 3. Khởi tạo ViewModel
            var model = new RevenueReportViewModel
            {
                CurrentRevenue = currentRevenue,
                CurrentOrders = currentOrders,
                TotalExpenses = currentExpenses, // Đã thêm thuộc tính này ở bước trước
                
                // Vì chưa có bảng OrderDetail, tạm thời để ProductsSold = 0
                ProductsSold = 0, 

                LastMonthRevenue = lastMonthRevenue,
                LastMonthOrders = lastMonthOrders,
                
                TopProducts = new List<TopProductStat>() // Cần bảng OrderDetail để tính chính xác
            };

            return View(model);
        }

        // GET: Xuất báo cáo Excel
        [HttpGet]
        public async Task<IActionResult> ExportRevenueReport()
        {
            var today = DateTime.Now;
            var currentMonth = today.Month;
            var currentYear = today.Year;

            var startOfMonth = new DateTime(currentYear, currentMonth, 1);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            // 1. Lấy dữ liệu thật từ DB
            var transactions = await _context.Transactions
                .Where(t => t.Date >= startOfMonth && t.Date < startOfNextMonth)
                .ToListAsync();

            // Nhóm dữ liệu theo ngày
            var dailyStats = transactions
                .GroupBy(t => t.Date.Date)
                .Select(g => new DailyStat
                {
                    Date = g.Key,
                    Revenue = g.Where(x => x.IsIncome).Sum(x => x.Amount),
                    Orders = g.Count(x => x.IsIncome)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // 2. Tạo Excel bằng EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"DoanhThu_Thang{currentMonth}");

                // --- HEADER BÁO CÁO ---
                worksheet.Cells["A1:E1"].Merge = true;
                worksheet.Cells["A1"].Value = $"BÁO CÁO DOANH THU THÁNG {currentMonth}/{currentYear}";
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // --- THÔNG TIN TỔNG QUAN ---
                decimal totalRevenue = dailyStats.Sum(x => x.Revenue);
                int totalOrders = dailyStats.Sum(x => x.Orders);

                worksheet.Cells["A3"].Value = "Tổng doanh thu:";
                worksheet.Cells["B3"].Value = totalRevenue;
                worksheet.Cells["B3"].Style.Numberformat.Format = "#,##0 ₫";
                worksheet.Cells["B3"].Style.Font.Bold = true;

                worksheet.Cells["A4"].Value = "Tổng đơn hàng (Giao dịch thu):";
                worksheet.Cells["B4"].Value = totalOrders;

                worksheet.Cells["A5"].Value = "Trung bình/ngày:";
                worksheet.Cells["B5"].Value = dailyStats.Any() ? dailyStats.Average(x => x.Revenue) : 0;
                worksheet.Cells["B5"].Style.Numberformat.Format = "#,##0 ₫";

                // --- BẢNG CHI TIẾT ---
                int tableStartRow = 8;
                
                worksheet.Cells[tableStartRow, 1].Value = "Ngày";
                worksheet.Cells[tableStartRow, 2].Value = "Số giao dịch thu";
                worksheet.Cells[tableStartRow, 3].Value = "Doanh thu (VNĐ)";
                worksheet.Cells[tableStartRow, 4].Value = "Giá trị trung bình";
                worksheet.Cells[tableStartRow, 5].Value = "Ghi chú";

                // Style Header
                using (var range = worksheet.Cells[tableStartRow, 1, tableStartRow, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Đổ dữ liệu
                int currentRow = tableStartRow + 1;
                if (dailyStats.Any())
                {
                    foreach (var item in dailyStats)
                    {
                        worksheet.Cells[currentRow, 1].Value = item.Date.ToString("dd/MM/yyyy");
                        worksheet.Cells[currentRow, 2].Value = item.Orders;
                        worksheet.Cells[currentRow, 3].Value = item.Revenue;
                        worksheet.Cells[currentRow, 3].Style.Numberformat.Format = "#,##0";
                        
                        worksheet.Cells[currentRow, 4].Formula = $"IF(B{currentRow}>0, C{currentRow}/B{currentRow}, 0)";
                        worksheet.Cells[currentRow, 4].Style.Numberformat.Format = "#,##0";

                        currentRow++;
                    }
                }
                else
                {
                    worksheet.Cells[currentRow, 1].Value = "Không có dữ liệu phát sinh trong tháng này.";
                    worksheet.Cells[currentRow, 1, currentRow, 5].Merge = true;
                    worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    currentRow++;
                }

                // --- DÒNG TỔNG CỘNG ---
                if (dailyStats.Any())
                {
                    worksheet.Cells[currentRow, 1].Value = "TỔNG CỘNG";
                    worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                    
                    worksheet.Cells[currentRow, 2].Formula = $"SUM(B{tableStartRow + 1}:B{currentRow - 1})";
                    worksheet.Cells[currentRow, 2].Style.Font.Bold = true;

                    worksheet.Cells[currentRow, 3].Formula = $"SUM(C{tableStartRow + 1}:C{currentRow - 1})";
                    worksheet.Cells[currentRow, 3].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 3].Style.Numberformat.Format = "#,##0 ₫";
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"BaoCaoDoanhThu_{currentMonth}_{currentYear}_{DateTime.Now:yyyyMMddHHmm}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}