using System;
using System.Collections.Generic;

namespace SpaBookingWeb.ViewModels.Manager
{
    public class SalaryPayrollViewModel
    {
        public int SalaryId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        
        public double TotalWorkHours { get; set; }
        public decimal BaseSalary { get; set; } // Lương cơ bản (tham chiếu)
        public decimal TotalCommission { get; set; } // Hoa hồng từ dịch vụ
        public decimal Bonus { get; set; }
        public decimal Deduction { get; set; } // Phạt/Khấu trừ
        
        public decimal FinalSalary { get; set; } // Thực nhận
        
        public string Status { get; set; } = "Draft"; // Draft, ManagerConfirmed, EmployeeConfirmed, Paid
    }
}