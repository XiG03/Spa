using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpaBookingWeb.ViewModels.Client
{
    // ViewModel tổng hợp cho trang Booking
    public class BookingPageViewModel
    {
        // 1. Danh sách dịch vụ để chọn (Nhóm theo Category)
        public List<ServiceCategoryGroupViewModel> ServiceCategories { get; set; } = new List<ServiceCategoryGroupViewModel>();

        // 2. Danh sách nhân viên để chọn
        public List<StaffViewModel> Staffs { get; set; } = new List<StaffViewModel>();

        // 3. Thông tin đặt lịch (Form submit)
        public BookingSubmissionModel Submission { get; set; } = new BookingSubmissionModel();
    }

    public class ServiceCategoryGroupViewModel
    {
        public string CategoryName { get; set; }
        public List<ServiceItemViewModel> Services { get; set; } = new List<ServiceItemViewModel>();
    }

    public class ServiceItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public string Type { get; set; } // "Service" hoặc "Combo"
        public bool IsSelected { get; set; }
    }

    public class StaffViewModel
    {
        public int Id { get; set; } // EmployeeId
        public string Name { get; set; }
        public string Role { get; set; } // VD: Senior Stylist, Nail Tech
        public string Avatar { get; set; }
    }

    // Model để nhận dữ liệu submit từ form
    public class BookingSubmissionModel
    {
        [Required(ErrorMessage = "Vui lòng chọn ít nhất một dịch vụ.")]
        public List<int> SelectedServiceIds { get; set; } = new List<int>(); // ID các dịch vụ/combo đã chọn

        public int? SelectedStaffId { get; set; } // Null nếu chọn "Any Staff"

        [Required(ErrorMessage = "Vui lòng chọn ngày giờ.")]
        public DateTime SelectedDate { get; set; } // Ngày hẹn

        [Required(ErrorMessage = "Vui lòng chọn giờ.")]
        public TimeSpan SelectedTime { get; set; } // Giờ hẹn

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string CustomerPhone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string CustomerEmail { get; set; }

        public string Note { get; set; }
    }
}