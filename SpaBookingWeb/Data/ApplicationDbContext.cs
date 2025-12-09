using Microsoft.AspNetCore.Identity; // Cần thêm dòng này
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaBookingWeb.Models;

namespace SpaBookingWeb.Data
{
    // Lưu ý: Đổi <Users> thành <ApplicationUser> nếu bạn dùng model tôi cung cấp trước đó
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<TechnicianDetail> TechnicianDetails { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<MembershipType> MembershipTypes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<ServiceConsumable> ServiceConsumables { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboDetail> ComboDetails { get; set; }
        public DbSet<TechnicianService> TechnicianServices { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<DepositRule> DepositRules { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentDetail> AppointmentDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AppointmentConsumable> AppointmentConsumables { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Salary> Salaries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cấu hình Khóa chính phức hợp (Composite Keys)
            builder.Entity<ServiceConsumable>().HasKey(sc => new { sc.ServiceId, sc.ProductId });
            builder.Entity<ComboDetail>().HasKey(cd => new { cd.ComboId, cd.ServiceId });
            builder.Entity<TechnicianService>().HasKey(ts => new { ts.EmployeeId, ts.ServiceId });

            // --- SỬA LỖI TẠI ĐÂY ---
            // Thay AspNetUserRole bằng IdentityUserRole<string>
            builder.Entity<IdentityUserRole<string>>().HasKey(r => new { r.UserId, r.RoleId });

            // Cấu hình Unique Constraints
            builder.Entity<Employee>()
                .HasIndex(e => e.IdentityUserId)
                .IsUnique();
            
            builder.Entity<Voucher>()
                .HasIndex(v => v.Code)
                .IsUnique();

            // Cấu hình quan hệ 1-1 cho Invoice - Appointment
            builder.Entity<Invoice>()
                .HasOne(i => i.Appointment)
                .WithOne(a => a.Invoice)
                .HasForeignKey<Invoice>(i => i.AppointmentId);

            // Cấu hình Delete Behavior
            builder.Entity<AppointmentDetail>()
                .HasOne(ad => ad.Appointment)
                .WithMany(a => a.AppointmentDetails)
                .HasForeignKey(ad => ad.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Tắt cascade cho các quan hệ phụ để tránh lỗi SQL Server
            builder.Entity<AppointmentDetail>()
                .HasOne(ad => ad.Technician)
                .WithMany(e => e.ServiceAppointments)
                .HasForeignKey(ad => ad.TechnicianId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}