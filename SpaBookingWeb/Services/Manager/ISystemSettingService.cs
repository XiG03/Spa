using SpaBookingWeb.ViewModels.Manager;
using System.Threading.Tasks;

namespace SpaBookingWeb.Services.Manager
{
    public interface ISystemSettingService
    {
        // 1. Cấu hình chung
        Task<SystemSettingViewModel> GetCurrentSettingsAsync();
        Task UpdateSettingsAsync(SystemSettingViewModel model);

        // 2. Quản lý Đơn vị tính (Units)
        Task AddUnitAsync(string unitName);
        Task DeleteUnitAsync(int id);
        
        // 3. Quản lý Vai trò (Roles)
        Task AddRoleAsync(string roleName);
        Task DeleteRoleAsync(string roleId);

        // --- MỚI: QUẢN LÝ QUYỀN HẠN (PERMISSIONS) ---
        // Lấy danh sách quyền (đã có/chưa có) của một Role để hiển thị lên bảng tích chọn
        Task<PermissionViewModel> GetPermissionsByRoleIdAsync(string roleId);
        
        // Lưu lại danh sách quyền đã chọn cho Role
        Task UpdatePermissionsAsync(PermissionViewModel model);
        // ---------------------------------------------

        // 4. Quản lý Quy tắc đặt cọc (Deposit Rules)
        Task DeleteDepositRuleAsync(int id);
    }
}