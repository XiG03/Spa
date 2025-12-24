using SpaBookingWeb.ViewModels.Manager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpaBookingWeb.Services.Manager
{
    public interface IEmployeeService
    {
        // 1. Quản lý nhân viên
        Task<List<EmployeeListViewModel>> GetAllEmployeesAsync();
        Task<EmployeeViewModel?> GetEmployeeByIdAsync(int id);
        Task CreateEmployeeAsync(EmployeeViewModel model); // Tạo cả User Identity và Employee
        Task UpdateEmployeeAsync(EmployeeViewModel model);
        Task DeleteEmployeeAsync(int id);

        // 2. Lịch làm việc
        Task<DailyScheduleViewModel> GetDailyScheduleAsync(DateTime date);
        Task AssignShiftAsync(int employeeId, int shiftId, DateTime date);
        
        // 3. Lương (Salary)
        Task<List<SalaryPayrollViewModel>> GeneratePayrollAsync(int month, int year);
        Task ConfirmSalaryByManagerAsync(int employeeId, int month, int year, decimal finalAmount);
        // Task ConfirmSalaryByEmployeeAsync(int salaryId); // Dành cho bên user cá nhân
    }
}