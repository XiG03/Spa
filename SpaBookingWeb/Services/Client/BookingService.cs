using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SpaBookingWeb.Data;
using SpaBookingWeb.Models;
using SpaBookingWeb.Services.Manager;
using SpaBookingWeb.ViewModels.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaBookingWeb.Services.Client
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISystemSettingService _systemSettingService;

        public BookingService(ApplicationDbContext context,
                              IHttpContextAccessor httpContextAccessor,
                              ISystemSettingService systemSettingService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _systemSettingService = systemSettingService;
        }

        // --- SESSION ---
        private ISession Session => _httpContextAccessor.HttpContext?.Session ?? throw new InvalidOperationException("Session is not available");

        public BookingSessionModel GetSession()
        {
            var json = Session.GetString("BookingSession");
            return json == null
            ? null : JsonConvert.DeserializeObject<BookingSessionModel>(json);
        }

        public void SaveSession(BookingSessionModel session)
        {
            var json = JsonConvert.SerializeObject(session);
            Session.SetString("BookingSession", json);
        }

        public void ClearSession()
        {
            Session.Remove("BookingSession");
        }

        public async Task<BookingPageViewModel> GetBookingPageDataAsync()
        {
            var model = new BookingPageViewModel();
            var services = await _context.Services.Include(s => s.Category).Where(s => s.IsActive && !s.IsDeleted).ToListAsync();
            model.ServiceCategories = services.GroupBy(s => s.Category?.CategoryName ?? "Khác").Select(g => new ServiceCategoryGroupViewModel { CategoryName = g.Key, Services = g.Select(s => new ServiceItemViewModel { Id = s.ServiceId, Name = s.ServiceName, Description = s.Description, Price = s.Price, DurationMinutes = s.DurationMinutes, Type = "Service" }).ToList() }).ToList();
            model.Staffs = await _context.Employees.Where(e => e.IsActive && !e.IsDeleted).Select(e => new StaffViewModel { Id = e.EmployeeId, Name = e.FullName, Role = "Kỹ thuật viên", Avatar = e.Avatar ?? "/img/default-avatar.png" }).ToListAsync();
            return model;
        }

        public async Task<List<string>> GetAvailableTimeSlotsAsync(DateTime date, BookingSessionModel session)
        {
            var settings = await _systemSettingService.GetCurrentSettingsAsync();
            var openTime = settings.OpenTime;
            var closeTime = settings.CloseTime;
            var slots = new List<string>();
            for (var time = openTime; time < closeTime; time = time.Add(TimeSpan.FromMinutes(30))) { slots.Add(time.ToString(@"hh\:mm")); }
            return slots;
        }

        public async Task<int> SaveBookingAsync(BookingSessionModel session)
        {
            // 1. Tạo/Lấy khách hàng
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == session.CustomerInfo.Phone);
            if (customer == null)
            {
                customer = new Customer { FullName = session.CustomerInfo.FullName, PhoneNumber = session.CustomerInfo.Phone, Email = session.CustomerInfo.Email };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // 2. Tạo Appointment
            var appointment = new Appointment
            {
                CustomerId = customer.CustomerId,
                StartTime = session.SelectedDate.Value.Add(session.SelectedTime.Value),
                EndTime = session.SelectedDate.Value.Add(session.SelectedTime.Value).AddMinutes(60),
                Status = "Pending", // Chờ thanh toán/xác nhận
                Notes = session.CustomerInfo.Note + (session.IsGroupBooking ? $" [Nhóm {session.Members.Count} người]" : ""),
                DepositAmount = session.DepositAmount,
                IsDepositPaid = false, // Mặc định chưa thanh toán cọc
                CreatedDate = DateTime.Now
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // 3. Tạo Appointment Details
            foreach (var member in session.Members)
            {
                foreach (var serviceId in member.SelectedServiceIds)
                {
                    // [SỬA LỖI] Lấy giá tiền của dịch vụ để gán vào PriceAtBooking
                    var servicePrice = await _context.Services
                        .Where(s => s.ServiceId == serviceId)
                        .Select(s => s.Price)
                        .FirstOrDefaultAsync();

                    // Xác định nhân viên thực hiện (nếu đã chọn)
                    int? selectedStaffId = null;
                    if (member.ServiceStaffMap != null && member.ServiceStaffMap.ContainsKey(serviceId))
                    {
                        selectedStaffId = member.ServiceStaffMap[serviceId];
                    }

                    _context.AppointmentDetails.Add(new AppointmentDetail
                    {
                        AppointmentId = appointment.AppointmentId,
                        ServiceId = serviceId,
                        TechnicianId = selectedStaffId,
                        PriceAtBooking = servicePrice, // Biến này giờ đã được khai báo ở trên
                        Status = "Pending"
                    });
                }
            }

            // 4. Tạo Invoice
            var invoice = new Invoice
            {
                AppointmentId = appointment.AppointmentId,
                TotalAmount = session.TotalAmount,
                DepositDeduction = session.DepositAmount,
                FinalAmount = session.TotalAmount - session.DepositAmount,
                PaymentStatus = "Unpaid",
                CreatedDate = DateTime.Now
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return appointment.AppointmentId;
        }

        // [MỚI] Cập nhật trạng thái sau khi thanh toán thành công
        public async Task UpdateDepositStatusAsync(int appointmentId, string transactionId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                // Cập nhật trạng thái Appointment
                appointment.IsDepositPaid = true;
                appointment.Status = "Confirmed"; // Đã cọc -> Xác nhận luôn

                // Cập nhật Invoice & Tạo Payment log
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.AppointmentId == appointmentId);
                if (invoice != null)
                {
                    invoice.PaymentStatus = "DepositPaid";

                    // Lưu lịch sử giao dịch
                    _context.Payments.Add(new Payment
                    {
                        InvoiceId = invoice.InvoiceId,
                        Amount = appointment.DepositAmount,
                        PaymentMethod = "Momo",
                        TransactionType = "Deposit", // Loại giao dịch: Đặt cọc
                        PaymentDate = DateTime.Now
                        // Bạn có thể lưu transactionId của MoMo vào trường ghi chú nếu muốn
                    });
                }

                await _context.SaveChangesAsync();
            }
        }

        // [MỚI] Lấy dữ liệu hiển thị trang Success
        public async Task<AppointmentSuccessViewModel> GetAppointmentSuccessInfoAsync(int appointmentId)
        {
            var app = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.AppointmentDetails).ThenInclude(ad => ad.Service)
                .Include(a => a.AppointmentDetails).ThenInclude(ad => ad.Technician)
                .Include(a => a.Invoice)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (app == null) return null;

            return new AppointmentSuccessViewModel
            {
                AppointmentId = app.AppointmentId,
                CustomerName = app.Customer.FullName,
                TimeSlot = $"{app.StartTime:HH:mm} - {app.StartTime:ddd, dd/MM}",
                Services = app.AppointmentDetails.Select(ad => new ServiceSuccessItem
                {
                    ServiceName = ad.Service?.ServiceName,
                    StaffName = ad.Technician?.FullName ?? "Bất kỳ ai",
                    Duration = ad.Service?.DurationMinutes ?? 0,
                    Price = ad.PriceAtBooking
                }).ToList(),
                TotalAmount = app.Invoice.TotalAmount,
                DepositAmount = app.DepositAmount,
                IsDepositPaid = app.IsDepositPaid
            };
        }
    }
}