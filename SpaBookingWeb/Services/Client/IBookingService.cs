using SpaBookingWeb.ViewModels.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpaBookingWeb.Services.Client
{
    public interface IBookingService
    {
        // Quản lý Session
        BookingSessionModel GetSession();
        void SaveSession(BookingSessionModel session);
        void ClearSession();

        // Lấy dữ liệu hiển thị
        Task<BookingPageViewModel> GetBookingPageDataAsync();

        // Logic nghiệp vụ
        Task<List<string>> GetAvailableTimeSlotsAsync(DateTime date, BookingSessionModel session);
        Task<int> SaveBookingAsync(BookingSessionModel session); // Trả về AppointmentId

        // [MỚI] Cập nhật trạng thái đã thanh toán cọc
        Task UpdateDepositStatusAsync(int appointmentId, string transactionId);
        // [MỚI] Lấy thông tin Appointment để hiển thị trang Success
        Task<AppointmentSuccessViewModel> GetAppointmentSuccessInfoAsync(int appointmentId);
        
    }
}