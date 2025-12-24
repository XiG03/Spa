using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpaBookingWeb.ViewModels.Manager
{
    // ViewModel cho danh sách nhân viên
    public class EmployeeListViewModel
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Position { get; set; } = "Technician"; // Vị trí (VD: Kỹ thuật viên)
        public string Avatar { get; set; } = string.Empty;
        
        // Thống kê
        public double AverageRating { get; set; } // Điểm đánh giá trung bình
        public int TotalAppointments { get; set; } // Tổng số ca đã phục vụ
        public bool IsActive { get; set; }
    }

    // ViewModel cho Thêm/Sửa nhân viên
    public class EmployeeViewModel
    {
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; } = "Nữ";

        [Required]
        public decimal BaseSalary { get; set; } // Lương cơ bản

        public DateTime HireDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }

    // ViewModel hiển thị lịch làm việc trong ngày
    public class DailyScheduleViewModel
    {
        public DateTime Date { get; set; }
        public List<ShiftAssignmentDto> Shifts { get; set; } = new List<ShiftAssignmentDto>();
    }

    public class ShiftAssignmentDto
    {
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty; // VD: 08:00 - 12:00
        public List<EmployeeListViewModel> WorkingEmployees { get; set; } = new List<EmployeeListViewModel>();
    }
}