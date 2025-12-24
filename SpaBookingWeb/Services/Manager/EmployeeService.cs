using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaBookingWeb.Data;
using SpaBookingWeb.Models;
using SpaBookingWeb.ViewModels.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaBookingWeb.Services.Manager
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- 1. QUẢN LÝ NHÂN VIÊN ---

        public async Task<List<EmployeeListViewModel>> GetAllEmployeesAsync()
        {
            var employees = await _context.Employees
                // SỬA: Dùng ApplicationUser (theo tên trong Model vừa sửa)
                .Include(e => e.ApplicationUser) 
                .ToListAsync();

            var viewModels = new List<EmployeeListViewModel>();

            foreach (var emp in employees)
            {
                var reviews = await _context.Appointments
                    .Where(a => a.EmployeeId == emp.EmployeeId && a.Status == "Completed")
                    .Join(_context.Reviews, a => a.AppointmentId, r => r.AppointmentId, (a, r) => r)
                    .ToListAsync();

                double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 5.0;

                viewModels.Add(new EmployeeListViewModel
                {
                    EmployeeId = emp.EmployeeId,
                    FullName = emp.FullName,
                    // SỬA: Truy cập ApplicationUser
                    Email = emp.ApplicationUser?.Email ?? "N/A", 
                    PhoneNumber = emp.ApplicationUser?.PhoneNumber ?? "N/A",
                    Avatar = emp.Avatar,
                    IsActive = emp.IsActive,
                    AverageRating = Math.Round(avgRating, 1),
                    TotalAppointments = await _context.Appointments.CountAsync(a => a.EmployeeId == emp.EmployeeId && a.Status == "Completed")
                });
            }

            return viewModels;
        }

        public async Task<EmployeeViewModel?> GetEmployeeByIdAsync(int id)
        {
            // SỬA: Include ApplicationUser
            var emp = await _context.Employees.Include(e => e.ApplicationUser).FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (emp == null) return null;

            return new EmployeeViewModel
            {
                EmployeeId = emp.EmployeeId,
                FullName = emp.FullName,
                // SỬA: Truy cập ApplicationUser
                Email = emp.ApplicationUser?.Email ?? "N/A",
                PhoneNumber = emp.ApplicationUser?.PhoneNumber ?? "N/A",
                Address = emp.Address,
                Gender = emp.Gender,
                DateOfBirth = emp.DateOfBirth,
                BaseSalary = emp.BaseSalary,
                HireDate = emp.HireDate,
                IsActive = emp.IsActive
            };
        }

        public async Task CreateEmployeeAsync(EmployeeViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, "Spa@123456");
            if (!result.Succeeded)
            {
                throw new Exception("Lỗi tạo tài khoản: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, "Employee");

            var employee = new Employee
            {
                IdentityUserId = user.Id,
                FullName = model.FullName,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Address = model.Address,
                HireDate = model.HireDate,
                BaseSalary = model.BaseSalary,
                Avatar = "/ManagerAssets/assets/avatars/face-1.jpg",
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEmployeeAsync(EmployeeViewModel model)
        {
            // SỬA: Include ApplicationUser
            var emp = await _context.Employees.Include(e => e.ApplicationUser).FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId);
            if (emp == null) throw new Exception("Không tìm thấy nhân viên");

            emp.FullName = model.FullName;
            emp.BaseSalary = model.BaseSalary;
            emp.Address = model.Address;
            emp.IsActive = model.IsActive;

            // SỬA: Truy cập ApplicationUser để update thông tin login
            if (emp.ApplicationUser != null)
            {
                emp.ApplicationUser.Email = model.Email;
                emp.ApplicationUser.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(emp.ApplicationUser);
            }

            _context.Employees.Update(emp);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp != null)
            {
                emp.IsActive = false;
                _context.Employees.Update(emp);
                await _context.SaveChangesAsync();
            }
        }

        // --- 2. LỊCH LÀM VIỆC ---

        public async Task<DailyScheduleViewModel> GetDailyScheduleAsync(DateTime date)
        {
            var shifts = await _context.Shifts.ToListAsync();
            var schedules = await _context.WorkSchedules
                .Include(ws => ws.Employee)
                // SỬA: Include ApplicationUser thông qua Employee
                .ThenInclude(e => e.ApplicationUser) 
                .Where(ws => ws.WorkDate.Date == date.Date)
                .ToListAsync();

            var viewModel = new DailyScheduleViewModel
            {
                Date = date,
                Shifts = new List<ShiftAssignmentDto>()
            };

            foreach (var shift in shifts)
            {
                var shiftDto = new ShiftAssignmentDto
                {
                    ShiftId = shift.ShiftId,
                    ShiftName = shift.ShiftName,
                    TimeRange = $"{shift.StartTime} - {shift.EndTime}",
                    WorkingEmployees = schedules
                        .Where(s => s.ShiftId == shift.ShiftId)
                        .Select(s => new EmployeeListViewModel
                        {
                            EmployeeId = s.Employee.EmployeeId,
                            FullName = s.Employee.FullName,
                            Position = "Kỹ thuật viên"
                        }).ToList()
                };
                viewModel.Shifts.Add(shiftDto);
            }

            return viewModel;
        }

        public async Task AssignShiftAsync(int employeeId, int shiftId, DateTime date)
        {
            var exists = await _context.WorkSchedules
                .AnyAsync(ws => ws.EmployeeId == employeeId && ws.ShiftId == shiftId && ws.WorkDate.Date == date.Date);

            if (!exists)
            {
                var schedule = new WorkSchedule
                {
                    EmployeeId = employeeId,
                    ShiftId = shiftId,
                    WorkDate = date,
                    IsCheckIn = false
                };
                _context.WorkSchedules.Add(schedule);
                await _context.SaveChangesAsync();
            }
        }

        // --- 3. TÍNH LƯƠNG ---

        public async Task<List<SalaryPayrollViewModel>> GeneratePayrollAsync(int month, int year)
        {
            var employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            var payrolls = new List<SalaryPayrollViewModel>();

            foreach (var emp in employees)
            {
                var existingSalary = await _context.Salaries
                    .FirstOrDefaultAsync(s => s.EmployeeId == emp.EmployeeId && s.Month == month && s.Year == year);

                if (existingSalary != null)
                {
                    payrolls.Add(MapToSalaryViewModel(existingSalary, emp.FullName));
                }
                else
                {
                    var workDays = await _context.WorkSchedules
                        .Where(ws => ws.EmployeeId == emp.EmployeeId && ws.WorkDate.Month == month && ws.WorkDate.Year == year && ws.IsCheckIn)
                        .CountAsync();
                    double totalHours = workDays * 8; 

                    var completedServices = await _context.Appointments
                        .Include(a => a.AppointmentDetails)
                        .Where(a => a.EmployeeId == emp.EmployeeId && a.Status == "Completed" && a.CreatedDate.Month == month && a.CreatedDate.Year == year)
                        .ToListAsync();
                    
                    decimal totalCommission = completedServices.Sum(a => a.AppointmentDetails.Sum(ad => ad.PriceAtBooking)) * 0.1m;

                    decimal hourlyRate = emp.BaseSalary / 26 / 8;
                    decimal salaryByHours = hourlyRate * (decimal)totalHours;
                    decimal finalSalary = salaryByHours + totalCommission;

                    payrolls.Add(new SalaryPayrollViewModel
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.FullName,
                        Month = month,
                        Year = year,
                        TotalWorkHours = totalHours,
                        BaseSalary = emp.BaseSalary,
                        TotalCommission = totalCommission,
                        Bonus = 0,
                        Deduction = 0,
                        FinalSalary = Math.Round(finalSalary, 0),
                        Status = "Draft"
                    });
                }
            }
            return payrolls;
        }

        public async Task ConfirmSalaryByManagerAsync(int employeeId, int month, int year, decimal finalAmount)
        {
            var salary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Month == month && s.Year == year);

            if (salary == null)
            {
                var workDays = await _context.WorkSchedules
                        .Where(ws => ws.EmployeeId == employeeId && ws.WorkDate.Month == month && ws.WorkDate.Year == year && ws.IsCheckIn)
                        .CountAsync();
                double totalHours = workDays * 8;

                var completedServices = await _context.Appointments
                        .Include(a => a.AppointmentDetails)
                        .Where(a => a.EmployeeId == employeeId && a.Status == "Completed" && a.CreatedDate.Month == month && a.CreatedDate.Year == year)
                        .ToListAsync();
                decimal totalCommission = completedServices.Sum(a => a.AppointmentDetails.Sum(ad => ad.PriceAtBooking)) * 0.1m;

                salary = new Salary
                {
                    EmployeeId = employeeId,
                    Month = month,
                    Year = year,
                    TotalWorkHours = totalHours,
                    TotalCommission = totalCommission,
                    Bonus = 0,
                    Deduction = 0,
                    TotalSalary = finalAmount,
                    Status = "ManagerConfirmed"
                };
                _context.Salaries.Add(salary);
            }
            else
            {
                salary.Status = "ManagerConfirmed";
                salary.TotalSalary = finalAmount;
                _context.Salaries.Update(salary);
            }

            await _context.SaveChangesAsync();
        }

        private SalaryPayrollViewModel MapToSalaryViewModel(Salary salary, string empName)
        {
            return new SalaryPayrollViewModel
            {
                SalaryId = salary.SalaryId,
                EmployeeId = salary.EmployeeId,
                EmployeeName = empName,
                Month = salary.Month,
                Year = salary.Year,
                TotalWorkHours = salary.TotalWorkHours,
                TotalCommission = salary.TotalCommission,
                Bonus = salary.Bonus,
                Deduction = salary.Deduction,
                FinalSalary = salary.TotalSalary,
                Status = salary.Status
            };
        }
    }
}