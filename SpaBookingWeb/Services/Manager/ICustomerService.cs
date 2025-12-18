using System.Collections.Generic;
using System.Threading.Tasks;
using SpaBookingWeb.ViewModels.Manager;
using SpaBookingWeb.Models;



namespace SpaBookingWeb.Services.Manager
{
    public interface ICustomerService
    {
        // Lấy dữ liệu cho Dashboard
        Task<CustomerDashboardViewModel> GetCustomerDashboardDataAsync();

        // CRUD Khách hàng
        Task<List<ApplicationUser>> GetAllCustomersAsync();
        Task<ApplicationUser> GetCustomerByIdAsync(string id);
        Task<bool> UpdateCustomerAsync(ApplicationUser user);
        Task<bool> DeleteCustomerAsync(string id); // Thường là khóa tài khoản (Soft Delete)
        Task<bool> CreateCustomerAsync(ApplicationUser user, string password);
    }
}


