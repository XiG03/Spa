using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

namespace SpaBookingWeb.Services.Manager
{
    public class ComboService : IComboService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ComboService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<ComboDashboardViewModel> GetAllCombosAsync()
        {
            // Lấy Combo kèm theo chi tiết dịch vụ
            var combos = await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.Service) // Giả sử model ComboDetails có nav prop Service
                .ToListAsync();

            var comboDtos = combos.Select(c => new ComboStatisticDto
            {
                ComboId = c.ComboId,
                ComboName = c.ComboName,
                Price = c.Price,
                Image = c.Image,
                ServiceCount = c.ComboDetails.Count,
                // Nối tên các dịch vụ thành chuỗi: "Massage, Xông hơi"
                ServiceNames = string.Join(", ", c.ComboDetails.Select(cd => cd.Service?.ServiceName))
            }).ToList();

            return new ComboDashboardViewModel { Combos = comboDtos };
        }

        public async Task<ComboViewModel> GetComboForCreateAsync()
        {
            // Lấy danh sách tất cả dịch vụ đang hoạt động để đổ vào Dropdown
            var services = await _context.Services.Where(s => s.IsActive).ToListAsync();

            return new ComboViewModel
            {
                AvailableServices = services.Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = $"{s.ServiceName} ({s.Price:N0}đ)"
                })
            };
        }

        public async Task<ComboViewModel?> GetComboForEditAsync(int id)
        {
            var combo = await _context.Combos
                .Include(c => c.ComboDetails)
                .FirstOrDefaultAsync(c => c.ComboId == id);

            if (combo == null) return null;

            var services = await _context.Services.Where(s => s.IsActive).ToListAsync();

            return new ComboViewModel
            {
                ComboId = combo.ComboId,
                ComboName = combo.ComboName,
                Price = combo.Price,
                Description = combo.Description,
                ExistingImage = combo.Image,
                // Lấy ra các ID dịch vụ đã được chọn trước đó
                SelectedServiceIds = combo.ComboDetails.Select(cd => cd.ServiceId).ToList(),
                AvailableServices = services.Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = $"{s.ServiceName} ({s.Price:N0}đ)"
                })
            };
        }

        public async Task<Combo?> GetComboByIdAsync(int id)
        {
            return await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.Service)
                .FirstOrDefaultAsync(c => c.ComboId == id);
        }

        public async Task CreateComboAsync(ComboViewModel model)
        {
            string imagePath = "/images/default-combo.jpg";
            if (model.ImageFile != null) imagePath = await SaveImageAsync(model.ImageFile);

            // 1. Tạo Combo
            var combo = new Combo
            {
                ComboName = model.ComboName,
                Price = model.Price,
                Description = model.Description ?? "",
                Image = imagePath
            };

            _context.Combos.Add(combo);
            await _context.SaveChangesAsync(); // Lưu để lấy ComboId

            // 2. Tạo ComboDetails (Liên kết dịch vụ)
            if (model.SelectedServiceIds != null && model.SelectedServiceIds.Any())
            {
                foreach (var serviceId in model.SelectedServiceIds)
                {
                    _context.ComboDetails.Add(new ComboDetail
                    {
                        ComboId = combo.ComboId,
                        ServiceId = serviceId
                    });
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateComboAsync(ComboViewModel model)
        {
            var combo = await _context.Combos.Include(c => c.ComboDetails).FirstOrDefaultAsync(c => c.ComboId == model.ComboId);
            if (combo == null) throw new Exception("Combo không tồn tại");

            if (model.ImageFile != null) combo.Image = await SaveImageAsync(model.ImageFile);

            combo.ComboName = model.ComboName;
            combo.Price = model.Price;
            combo.Description = model.Description ?? "";

            // --- Xử lý cập nhật danh sách dịch vụ ---
            
            // 1. Xóa hết các liên kết cũ
            _context.ComboDetails.RemoveRange(combo.ComboDetails);
            
            // 2. Thêm lại các liên kết mới từ danh sách chọn
            if (model.SelectedServiceIds != null)
            {
                foreach (var serviceId in model.SelectedServiceIds)
                {
                    _context.ComboDetails.Add(new ComboDetail
                    {
                        ComboId = combo.ComboId,
                        ServiceId = serviceId
                    });
                }
            }

            _context.Combos.Update(combo);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteComboAsync(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo != null)
            {
                // Vì bảng Combo trong SQL của bạn không có cột IsDeleted/IsActive
                // Nên chúng ta dùng xóa cứng (Hard Delete). 
                // ComboDetails sẽ tự động bị xóa theo (Cascade Delete) nếu đã cấu hình trong DBContext
                _context.Combos.Remove(combo);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "combos");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            string filePath = Path.Combine(uploadFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return "/images/combos/" + uniqueFileName;
        }
    }
}