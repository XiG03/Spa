using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaBookingWeb.Services.Manager;
using SpaBookingWeb.ViewModels.Manager;
using System.Threading.Tasks;

namespace SpaBookingWeb.Areas.Manager.Controllers
{
    [Area("Manager")]
    // [Authorize(Roles = "Manager,Admin")]
    public class SystemSettingController : Controller
    {
        private readonly ISystemSettingService _systemSettingService;

        public SystemSettingController(ISystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _systemSettingService.GetCurrentSettingsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SystemSettingViewModel model)
        {
            // ModelState có thể invalid do các field Unit/Role list bị null khi post về
            // Chúng ta chỉ cần validate các field chính của setting
            if (ModelState.IsValid)
            {
                await _systemSettingService.UpdateSettingsAsync(model);
                TempData["SuccessMessage"] = "Đã lưu cấu hình chung!";
                return RedirectToAction(nameof(Index));
            }
            
            // Nếu lỗi, load lại list để hiển thị
            var reloadModel = await _systemSettingService.GetCurrentSettingsAsync();
            // Merge dữ liệu form vào model reload
            reloadModel.SpaName = model.SpaName; 
            // ... (merge các field khác nếu cần)

            return View(reloadModel);
        }

        // --- UNIT ACTIONS ---
        [HttpPost]
        public async Task<IActionResult> AddUnit(string newUnitName)
        {
            await _systemSettingService.AddUnitAsync(newUnitName);
            TempData["SuccessMessage"] = "Đã thêm đơn vị tính.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            await _systemSettingService.DeleteUnitAsync(id);
            TempData["SuccessMessage"] = "Đã xóa đơn vị tính.";
            return RedirectToAction(nameof(Index));
        }

        // --- ROLE ACTIONS ---
        [HttpPost]
        public async Task<IActionResult> AddRole(string newRoleName)
        {
            await _systemSettingService.AddRoleAsync(newRoleName);
            TempData["SuccessMessage"] = "Đã thêm quyền mới.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(string id)
        {
            await _systemSettingService.DeleteRoleAsync(id);
            TempData["SuccessMessage"] = "Đã xóa quyền.";
            return RedirectToAction(nameof(Index));
        }

        // --- DEPOSIT RULE DELETE ---
        [HttpPost]
        public async Task<IActionResult> DeleteDepositRule(int id)
        {
            await _systemSettingService.DeleteDepositRuleAsync(id);
            TempData["SuccessMessage"] = "Đã xóa quy tắc đặt cọc.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ManagePermissions(string roleId)
        {
            var model = await _systemSettingService.GetPermissionsByRoleIdAsync(roleId);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vai trò này.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagePermissions(PermissionViewModel model)
        {
            await _systemSettingService.UpdatePermissionsAsync(model);
            TempData["SuccessMessage"] = $"Đã cập nhật quyền hạn cho nhóm {model.RoleName}.";
            return RedirectToAction(nameof(Index)); // Quay về trang cấu hình
        }
    }
}