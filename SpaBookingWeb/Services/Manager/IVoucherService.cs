using System.Collections.Generic;
using System.Threading.Tasks;
using SpaBookingWeb.ViewModels.Manager;
using SpaBookingWeb.Models;

namespace SpaBookingWeb.Services.Manager
{
    public interface IVoucherService
    {
        Task<VoucherDashboardViewModel> GetAllVouchersAsync();
        Task<Voucher> GetVoucherByIdAsync(int id);
        Task CreateVoucherAsync(CreateVoucherViewModel model);
        Task UpdateVoucherAsync(CreateVoucherViewModel model);
        Task DeleteVoucherAsync(int id);
    }
}

