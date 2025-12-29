using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SpaBookingWeb.Data;
using SpaBookingWeb.Models;
using SpaBookingWeb.ViewModels.Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Threading.Tasks;
using SpaBookingWeb.Constants;
using System.Security.Claims;

namespace SpaBookingWeb.Services.Manager
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SystemSettingService(ApplicationDbContext context, 
                                    IWebHostEnvironment webHostEnvironment,
                                    RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _roleManager = roleManager;
        }

        // --- 1. LOGIC CẤU HÌNH CHUNG ---
        public async Task<SystemSettingViewModel> GetCurrentSettingsAsync()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            var settingsDict = settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            string GetValue(string key, string defaultValue = "") => 
                settingsDict.ContainsKey(key) ? settingsDict[key] : defaultValue;

            var model = new SystemSettingViewModel
            {
                SpaName = GetValue("SpaName", "Spa System"),
                PhoneNumber = GetValue("PhoneNumber"),
                Email = GetValue("Email"),
                Address = GetValue("Address"),
                LogoUrl = GetValue("LogoUrl"),
                FacebookUrl = GetValue("FacebookUrl"),
                OpenTime = TimeSpan.TryParse(GetValue("OpenTime"), out var open) ? open : new TimeSpan(8, 0, 0),
                CloseTime = TimeSpan.TryParse(GetValue("CloseTime"), out var close) ? close : new TimeSpan(22, 0, 0),
                DepositPercentage = int.TryParse(GetValue("DepositPercentage"), out var dep) ? dep : 30
            };

            model.Units = await _context.Units.ToListAsync();
            model.Roles = await _roleManager.Roles.ToListAsync();
            model.DepositRules = await _context.DepositRules
                .Include(d => d.TargetService)
                .Include(d => d.TargetMembershipType)
                .ToListAsync();

            // Load Dropdown Data
            var services = await _context.Services.Where(s => s.IsActive).ToListAsync();
            model.AvailableServices = services.Select(s => new SelectListItem
            {
                Value = s.ServiceId.ToString(),
                Text = $"{s.ServiceName} ({s.Price:N0}đ)"
            });

            var membershipTypes = await _context.MembershipTypes.ToListAsync();
            model.AvailableMembershipTypes = membershipTypes.Select(m => new SelectListItem
            {
                Value = m.MembershipTypeId.ToString(),
                Text = m.TypeName
            });

            return model;
        }

        public async Task UpdateSettingsAsync(SystemSettingViewModel model)
        {
            string logoPath = model.LogoUrl;
            if (model.LogoFile != null)
            {
                logoPath = await SaveImageAsync(model.LogoFile);
            }

            var valuesToUpdate = new Dictionary<string, string>
            {
                { "SpaName", model.SpaName },
                { "PhoneNumber", model.PhoneNumber },
                { "Email", model.Email },
                { "Address", model.Address },
                { "LogoUrl", logoPath },
                { "FacebookUrl", model.FacebookUrl },
                { "OpenTime", model.OpenTime.ToString() },
                { "CloseTime", model.CloseTime.ToString() },
                { "DepositPercentage", model.DepositPercentage.ToString() }
            };

            foreach (var kvp in valuesToUpdate)
            {
                var setting = await _context.SystemSettings.FindAsync(kvp.Key);
                if (setting == null)
                {
                    setting = new SystemSetting { SettingKey = kvp.Key, SettingValue = kvp.Value ?? "", Description = $"Cấu hình {kvp.Key}" };
                    _context.SystemSettings.Add(setting);
                }
                else
                {
                    setting.SettingValue = kvp.Value ?? "";
                }
            }
            await _context.SaveChangesAsync();
        }

        // --- 2. LOGIC QUẢN LÝ QUYỀN (PERMISSIONS) ---
        public async Task<PermissionViewModel> GetPermissionsByRoleIdAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return null;

            var model = new PermissionViewModel
            {
                RoleId = roleId,
                RoleName = role.Name,
                RoleClaims = new List<RoleClaimsDto>()
            };

            var allPermissions = Permissions.GetAllPermissions();
            var currentClaims = await _roleManager.GetClaimsAsync(role);
            var currentPermissions = currentClaims.Select(c => c.Value).ToList();

            foreach (var permission in allPermissions)
            {
                model.RoleClaims.Add(new RoleClaimsDto
                {
                    Value = permission,
                    Type = "Permission",
                    IsSelected = currentPermissions.Contains(permission)
                });
            }
            return model;
        }

        public async Task UpdatePermissionsAsync(PermissionViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return;

            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            var selectedClaims = model.RoleClaims.Where(a => a.IsSelected).ToList();
            foreach (var claim in selectedClaims)
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", claim.Value));
            }

            // Ghi Log
            var permissionsString = string.Join(", ", selectedClaims.Select(c => c.Value.Replace("Permissions.", "")));
            var log = new ActivityLog
            {
                Action = "Update",
                EntityName = "IdentityRole",
                EntityId = role.Id,
                Description = $"Cập nhật quyền cho Role '{role.Name}': {permissionsString}",
                AffectedColumns = "[]"
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // --- 3. CÁC HÀM CRUD PHỤ TRỢ (Unit, Role, DepositRule) ---
        public async Task AddUnitAsync(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName)) return;
            var unit = new Unit { UnitName = unitName };
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUnitAsync(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit != null)
            {
                _context.Units.Remove(unit); 
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return;
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                
                var log = new ActivityLog 
                { 
                    Action = "Create", EntityName = "IdentityRole", EntityId = roleName,
                    Description = $"Tạo Role mới: {roleName}", AffectedColumns = "[]"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
                var log = new ActivityLog
                {
                    Action = "Delete", EntityName = "IdentityRole", EntityId = roleId,
                    Description = $"Xóa Role: {role.Name}", AffectedColumns = "[]"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteDepositRuleAsync(int id)
        {
            var rule = await _context.DepositRules.FindAsync(id);
            if (rule != null)
            {
                _context.DepositRules.Remove(rule);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            string uniqueFileName = "logo_" + Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "system");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            string filePath = Path.Combine(uploadFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return "/images/system/" + uniqueFileName;
        }
    }
}