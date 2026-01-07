using System;
using System.Collections.Generic;
using SpaBookingWeb.Models;

namespace SpaBookingWeb.ViewModels.Manager
{
    public class CustomerDashboardViewModel
    {

        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalVisitsThisMonth { get; set; }
        public int CancelledOrdersThisMonth { get; set; }

        // Chỉ số so sánh
        public double ReturnRateThisMonth { get; set; } // Tỉ lệ quay lại (%)
        public double ReturnRateLastMonth { get; set; } // Tỉ lệ tháng trước để so sánh
        public double GrowthRate { get; set; } // Tỉ lệ tăng trưởng khách so với tháng trước

        // Danh sách khách hàng cho bảng dữ liệu
        public List<ApplicationUser> Customers { get; set; }

    }

    public class CustomerDetailViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime CreatedDate { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
    }
}


