using SpaBookingWeb.ViewModels.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpaBookingWeb.Services.Client
{
    public interface IBookingService
    {
        // Lấy dữ liệu khởi tạo cho trang booking (Danh sách dịch vụ, nhân viên)
        Task<BookingPageViewModel> GetBookingPageDataAsync();

        // Kiểm tra khung giờ trống cho nhân viên vào ngày cụ thể
        Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(DateTime date, int? staffId, int totalDuration);

        // Lưu booking mới
        Task<int> CreateBookingAsync(BookingSubmissionModel model, string userId = null);
    }
}