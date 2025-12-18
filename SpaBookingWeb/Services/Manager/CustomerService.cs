using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaBookingWeb.ViewModels.Manager;
using SpaBookingWeb.Data;
using SpaBookingWeb.Models;



namespace SpaBookingWeb.Services.Manager
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Định nghĩa tên Role cho Khách hàng
        private const string ROLE_CUSTOMER = "Customer";

        public CustomerService(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<CustomerDashboardViewModel> GetCustomerDashboardDataAsync()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);

            // 1. LẤY DANH SÁCH KHÁCH HÀNG (Lọc theo Role)
            // Lưu ý: GetUsersInRoleAsync sẽ chỉ trả về những user có role là Customer
            var allUsersInRole = await _userManager.GetUsersInRoleAsync(ROLE_CUSTOMER);

            // Chỉ lấy user có LockoutEnd là null hoặc thời gian khóa đã qua (tức là đang active)
            var customersList = allUsersInRole
                .Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.Now)
                .ToList();

            // Nếu hệ thống chưa có role Customer hoặc chưa gán role, danh sách này sẽ rỗng.
            // Lúc này ta coi như tổng khách = 0
            var totalCustomers = customersList.Count;

            // 2. Khách hàng mới trong tháng (Lọc từ danh sách customersList đã lấy ở trên)
            // Sử dụng trường CreatedDate mà ta đã thêm vào Model
            var newCustomersCount = customersList.Count(u => u.CreatedDate >= startOfMonth);

            // 3. Truy vấn bảng Operations (Lịch sử đặt/Giao dịch)
            var operationsThisMonth = await _context.Operations
                .Where(o => o.CreateDate >= startOfMonth)
                .Include(o => o.User) // Include để check role nếu cần, nhưng ở đây ta check theo UserId
                .ToListAsync();

            var operationsLastMonth = await _context.Operations
                .Where(o => o.CreateDate >= startOfLastMonth && o.CreateDate <= endOfLastMonth)
                .ToListAsync();

            // 4. Số lượng lượt ghé (Status = 1: Hoàn thành)
            // Chỉ tính lượt ghé của những người là Customer (đề phòng nhân viên test book lịch)
            // Lấy danh sách ID của khách hàng để đối chiếu
            var customerIds = customersList.Select(c => c.Id).ToHashSet();

            var completedVisits = operationsThisMonth
                .Count(o => o.Status == 1 && customerIds.Contains(o.UserId));

            // 5. Số lượng hủy (Status = -1)
            var cancelledOrders = operationsThisMonth
                .Count(o => o.Status == -1 && customerIds.Contains(o.UserId));

            // 6. Tính tỉ lệ quay lại (Retention Rate)
            double returnRateThisMonth = 0;

            // Nhóm các đơn hàng thành công theo UserId
            var customerVisitsThisMonth = operationsThisMonth
                .Where(o => o.Status == 1 && customerIds.Contains(o.UserId)) // Chỉ tính User là Customer
                .GroupBy(o => o.UserId);

            if (customerVisitsThisMonth.Any())
            {
                // Khách quay lại là khách có > 1 đơn thành công trong tháng
                double returningCustomers = customerVisitsThisMonth.Count(g => g.Count() > 1);
                double totalActiveCustomers = customerVisitsThisMonth.Count();

                if (totalActiveCustomers > 0)
                {
                    returnRateThisMonth = (returningCustomers / totalActiveCustomers) * 100;
                }
            }

            // Tỉ lệ tháng trước (để so sánh)
            double returnRateLastMonth = 0;
            var customerVisitsLastMonth = operationsLastMonth
                .Where(o => o.Status == 1 && customerIds.Contains(o.UserId))
                .GroupBy(o => o.UserId);

            if (customerVisitsLastMonth.Any())
            {
                double returningCustomersLast = customerVisitsLastMonth.Count(g => g.Count() > 1);
                double totalActiveCustomersLast = customerVisitsLastMonth.Count();

                if (totalActiveCustomersLast > 0)
                {
                    returnRateLastMonth = (returningCustomersLast / totalActiveCustomersLast) * 100;
                }
            }

            // 7. Tỉ lệ tăng trưởng visits
            double growthRate = 0;
            // Tính số visits tháng trước của Customer
            var visitsLastMonthCount = operationsLastMonth.Count(o => o.Status == 1 && customerIds.Contains(o.UserId));

            if (visitsLastMonthCount > 0)
            {
                growthRate = ((double)(completedVisits - visitsLastMonthCount) / visitsLastMonthCount) * 100;
            }

            // Xử lý hiển thị tên (Fallback nếu FullName null)
            foreach (var user in customersList)
            {
                if (string.IsNullOrEmpty(user.FullName)) user.FullName = user.UserName;
            }

            return new CustomerDashboardViewModel
            {
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newCustomersCount,
                TotalVisitsThisMonth = completedVisits,
                CancelledOrdersThisMonth = cancelledOrders,
                ReturnRateThisMonth = Math.Round(returnRateThisMonth, 2),
                ReturnRateLastMonth = Math.Round(returnRateLastMonth, 2),
                GrowthRate = Math.Round(growthRate, 2),
                // Chỉ hiển thị danh sách User thuộc role Customer
                Customers = customersList.OrderByDescending(u => u.CreatedDate).Take(100).ToList()
            };
        }

        public async Task<bool> CreateCustomerAsync(ApplicationUser user, string password)
        {
            // Gán username bằng email nếu chưa có
            if (string.IsNullOrEmpty(user.UserName)) user.UserName = user.Email;
            user.CreatedDate = DateTime.Now;
            user.EmailConfirmed = true; // Admin tạo thì mặc định confirm luôn để tiện login

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Đảm bảo Role tồn tại
                if (!await _roleManager.RoleExistsAsync(ROLE_CUSTOMER))
                {
                    await _roleManager.CreateAsync(new IdentityRole(ROLE_CUSTOMER));
                }

                await _userManager.AddToRoleAsync(user, ROLE_CUSTOMER);
                return true;
            }
            return false;
        }

        // Chỉ lấy user là Customer
        public async Task<List<ApplicationUser>> GetAllCustomersAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync(ROLE_CUSTOMER);
            return users.ToList();
        }

        public async Task<ApplicationUser> GetCustomerByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            // Kiểm tra xem user này có phải là Customer không
            // Nếu là Manager/Admin thì không trả về ở đây (để tránh Manager edit nhầm Manager khác trong giao diện Customer)
            var isCustomer = await _userManager.IsInRoleAsync(user, ROLE_CUSTOMER);

            if (!isCustomer) return null;

            return user;
        }

        public async Task<bool> UpdateCustomerAsync(ApplicationUser user)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null) return false;

            // Double check role
            if (!await _userManager.IsInRoleAsync(existingUser, ROLE_CUSTOMER)) return false;

            existingUser.FullName = user.FullName;
            existingUser.PhoneNumber = user.PhoneNumber;
            existingUser.Email = user.Email;
            existingUser.Address = user.Address;

            var result = await _userManager.UpdateAsync(existingUser);
            return result.Succeeded;
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            // Double check role
            if (!await _userManager.IsInRoleAsync(user, ROLE_CUSTOMER)) return false;

             user.LockoutEnabled = true; // Đảm bảo tính năng khóa được bật
            user.LockoutEnd = DateTimeOffset.MaxValue; // Khóa đến ngày tận thế -> Coi như xóa

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}

