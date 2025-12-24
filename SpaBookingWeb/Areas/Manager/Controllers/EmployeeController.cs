using Microsoft.AspNetCore.Mvc;
using SpaBookingWeb.Services.Manager;
using SpaBookingWeb.ViewModels.Manager;
using System;
using System.Threading.Tasks;

namespace SpaBookingWeb.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        // --- 1. DANH SÁCH NHÂN VIÊN ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _employeeService.GetAllEmployeesAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _employeeService.CreateEmployeeAsync(model);
                    TempData["Success"] = "Tạo nhân viên thành công";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(model);
        }

        // --- 2. LỊCH LÀM VIỆC (Daily Schedule) ---
        [HttpGet]
        public async Task<IActionResult> Schedule(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var model = await _employeeService.GetDailyScheduleAsync(selectedDate);
            return View(model);
        }

        // --- 3. TÍNH LƯƠNG (Payroll) ---
        [HttpGet]
        public async Task<IActionResult> Payroll(int? month, int? year)
        {
            var m = month ?? DateTime.Now.Month;
            var y = year ?? DateTime.Now.Year;

            ViewBag.Month = m;
            ViewBag.Year = y;

            var payrolls = await _employeeService.GeneratePayrollAsync(m, y);
            return View(payrolls);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmSalary(int employeeId, int month, int year, decimal finalAmount)
        {
            // Logic lưu lương vào DB với Status = "ManagerConfirmed"
            // Sau khi Manager confirm, nhân viên sẽ thấy ở trang cá nhân của họ
            TempData["Success"] = "Đã xác nhận bảng lương cho nhân viên.";
            return RedirectToAction(nameof(Payroll), new { month, year });
        }
    }
}