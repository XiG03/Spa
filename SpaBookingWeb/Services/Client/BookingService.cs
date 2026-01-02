using Microsoft.EntityFrameworkCore;
using SpaBookingWeb.Data;
using SpaBookingWeb.Models;
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

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookingPageViewModel> GetBookingPageDataAsync()
        {
            var model = new BookingPageViewModel();

            // 1. Lấy danh sách Dịch vụ (Service)
            var services = await _context.Services
                .Include(s => s.Category)
                .Where(s => s.IsActive && !s.IsDeleted)
                .ToListAsync();

            // Nhóm dịch vụ theo Category
            var groupedServices = services
                .GroupBy(s => s.Category?.CategoryName ?? "Khác")
                .Select(g => new ServiceCategoryGroupViewModel
                {
                    CategoryName = g.Key,
                    Services = g.Select(s => new ServiceItemViewModel
                    {
                        Id = s.ServiceId,
                        Name = s.ServiceName,
                        Description = s.Description,
                        Price = s.Price,
                        DurationMinutes = s.DurationMinutes,
                        Type = "Service"
                    }).ToList()
                }).ToList();

            // Có thể thêm Combo vào nhóm riêng nếu muốn
            var combos = await _context.Combos.Where(c => !c.IsDeleted).ToListAsync();
            if (combos.Any())
            {
                groupedServices.Insert(0, new ServiceCategoryGroupViewModel
                {
                    CategoryName = "Combo & Gói Dịch Vụ",
                    Services = combos.Select(c => new ServiceItemViewModel
                    {
                        Id = c.ComboId, // Lưu ý: Cần xử lý logic ID để phân biệt Service và Combo (ví dụ dùng prefix hoặc negative ID)
                        Name = c.ComboName,
                        Description = c.Description,
                        Price = c.Price,
                        // DurationMinutes = ... (Tính tổng duration của combo nếu cần)
                        DurationMinutes = 60, // Tạm thời hardcode hoặc tính toán từ ComboDetails
                        Type = "Combo"
                    }).ToList()
                });
            }

            model.ServiceCategories = groupedServices;

            // 2. Lấy danh sách Nhân viên (Staff)
            var employees = await _context.Employees
                .Where(e => e.IsActive && !e.IsDeleted)
                .Select(e => new StaffViewModel
                {
                    Id = e.EmployeeId,
                    Name = e.FullName,
                    Role = "Kỹ thuật viên", // Hoặc lấy từ TechnicianDetail
                    Avatar = e.Avatar ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuASiaYq9pFRIPi507zEhqW_PMoFXkzaX6pDRv6ULDlqjtSVXPxeT7CxZERJHhZZNrAKqgAvXZdebiOeaxv_xFiXNBlSBVAca7I1owHT8C-C2Xw36lF_uIzKQ8iKYDReM8Op3USARj8ymsswxzYHN-DdW6tV1s4onET0rRQZsQ0LmxOYw3a1RwB-C1JmdJx7pNakp6QBEXiasOcUmVO-rqTdQLy22fg4w3TpVLrWPB-NuLj3OEmCSudFPBzc7cfpbQaXIHRoTU_nd6A" // Placeholder
                })
                .ToListAsync();

            model.Staffs = employees;

            return model;
        }

        public async Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(DateTime date, int? staffId, int totalDuration)
        {
            // Logic đơn giản: Trả về các khung giờ cố định. 
            // Thực tế cần check lịch làm việc (WorkSchedule) và các Appointment đã có để loại trừ.
            var slots = new List<TimeSpan>();
            var startTime = new TimeSpan(9, 0, 0); // 9:00 AM
            var endTime = new TimeSpan(20, 0, 0); // 8:00 PM

            while (startTime.Add(TimeSpan.FromMinutes(totalDuration)) <= endTime)
            {
                // TODO: Check database xem giờ này staff có bận không
                slots.Add(startTime);
                startTime = startTime.Add(TimeSpan.FromMinutes(30)); // Bước nhảy 30 phút
            }

            return await Task.FromResult(slots);
        }

        public async Task<int> CreateBookingAsync(BookingSubmissionModel model, string userId = null)
        {
            // 1. Tìm hoặc tạo Customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == model.CustomerPhone);
            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = model.CustomerName,
                    PhoneNumber = model.CustomerPhone,
                    Email = model.CustomerEmail,
                    // MembershipTypeId = ... (Mặc định)
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // 2. Tạo Appointment
            var appointment = new Appointment
            {
                CustomerId = customer.CustomerId,
                EmployeeId = model.SelectedStaffId,
                StartTime = model.SelectedDate.Date + model.SelectedTime,
                // EndTime tính toán dựa trên tổng duration dịch vụ
                Status = "Pending",
                Notes = model.Note,
                CreatedDate = DateTime.Now,
                IsDepositPaid = false
            };

            // Tính tổng tiền và thời gian
            decimal totalAmount = 0;
            int totalDuration = 0;

            // Xử lý Services/Combos đã chọn
            // Lưu ý: Logic này đang giả định model.SelectedServiceIds chứa ID của Service. 
            // Nếu có cả Combo, cần logic tách biệt ID.
            
            // Tạm thời xử lý Service
            var selectedServices = await _context.Services.Where(s => model.SelectedServiceIds.Contains(s.ServiceId)).ToListAsync();
            
            appointment.AppointmentDetails = new List<AppointmentDetail>();
            foreach (var service in selectedServices)
            {
                appointment.AppointmentDetails.Add(new AppointmentDetail
                {
                    ServiceId = service.ServiceId,
                    PriceAtBooking = service.Price,
                    TechnicianId = model.SelectedStaffId // Tạm gán staff chính cho tất cả dịch vụ
                });
                totalAmount += service.Price;
                totalDuration += service.DurationMinutes;
            }

            appointment.EndTime = appointment.StartTime.AddMinutes(totalDuration);

            // Tạo Invoice dự kiến (Optional)
            appointment.Invoice = new Invoice
            {
                TotalAmount = totalAmount,
                FinalAmount = totalAmount,
                PaymentStatus = "Unpaid",
                CreatedDate = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return appointment.AppointmentId;
        }
    }
}