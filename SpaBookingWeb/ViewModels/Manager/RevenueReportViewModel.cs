using System;
using System.Collections.Generic;

namespace SpaBookingWeb.ViewModels.Manager
{
    public class RevenueReportViewModel
    {
        // --- CHỈ SỐ THÁNG NAY ---
        public decimal CurrentRevenue { get; set; }
        public int CurrentOrders { get; set; }
        public int ProductsSold { get; set; }
        public decimal AverageOrderValue => CurrentOrders > 0 ? CurrentRevenue / CurrentOrders : 0;

        // --- CHỈ SỐ THÁNG TRƯỚC (ĐỂ SO SÁNH) ---
        public decimal LastMonthRevenue { get; set; }
        public int LastMonthOrders { get; set; }

        public decimal TotalExpenses { get; set; } // Tổng chi phí trong tháng hiện tại

        // --- TÍNH TOÁN TĂNG TRƯỞNG (%) ---
        public double RevenueGrowth
        {
            get
            {
                if (LastMonthRevenue == 0) return 100; // Nếu tháng trước = 0 thì tăng trưởng 100%
                return (double)((CurrentRevenue - LastMonthRevenue) / LastMonthRevenue) * 100;
            }
        }

        public double OrderGrowth
        {
            get
            {
                if (LastMonthOrders == 0) return 100;
                return (double)((CurrentOrders - LastMonthOrders) / (double)LastMonthOrders) * 100;
            }
        }

        // --- DỮ LIỆU CHI TIẾT ---
        public List<DailyStat> DailyStats { get; set; } = new List<DailyStat>();
        public List<TopProductStat> TopProducts { get; set; } = new List<TopProductStat>();
    }

    public class DailyStat
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class TopProductStat
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}