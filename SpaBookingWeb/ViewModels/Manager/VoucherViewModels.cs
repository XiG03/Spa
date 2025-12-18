using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpaBookingWeb.ViewModels.Manager
{
    public class VoucherDashboardViewModel
    {
        public List<VoucherViewModel> Vouchers { get; set; }
        public int ActiveCount { get; set; }
        public int TotalUsed { get; set; }
    }

    public class VoucherViewModel
    {
        public int VoucherId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string ValueDisplay { get; set; }
        public string MinSpendDisplay { get; set; }
        public string DateRange { get; set; }
        public string UsageStatus { get; set; } // Hiển thị dạng "10/100"
        public bool IsActive { get; set; }
    }

    public class CreateVoucherViewModel
    {
        public int VoucherId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên voucher")]
        [Display(Name = "Tên Voucher")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã voucher")]
        [StringLength(50, ErrorMessage = "Mã tối đa 50 ký tự")]
        [Display(Name = "Mã Code")]
        public string Code { get; set; }

        [Display(Name = "Mô Tả")]
        public string Description { get; set; }

        [Display(Name = "Loại Giảm Giá")]
        public string DiscountType { get; set; } // Select: "Percent" hoặc "Amount"

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Giá trị phải lớn hơn 0")]
        [Display(Name = "Giá Trị Giảm")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Giảm Tối Đa (VNĐ)")]
        public decimal? MaxDiscountAmount { get; set; }

        [Display(Name = "Đơn Tối Thiểu (VNĐ)")]
        public decimal MinSpend { get; set; }

        [Display(Name = "Giới hạn số lượt dùng")]
        public int UsageLimit { get; set; } = 100;

        [Required]
        [Display(Name = "Ngày Bắt Đầu")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Ngày Kết Thúc")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        public bool IsActive { get; set; } = true;
    }
}

