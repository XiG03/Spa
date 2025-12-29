using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using SpaBookingWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // Mới: Cần cho Reflection
using System.Threading;
using System.Threading.Tasks;

namespace SpaBookingWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // --- CÁC DBSET (GIỮ NGUYÊN) ---
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
        public DbSet<Operations> Operations { get; set; }
        public DbSet<TransactionCategory> TransactionCategories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ... (Giữ nguyên cấu hình cũ) ...
            builder.Entity<ServiceConsumable>().HasKey(sc => new { sc.ServiceId, sc.ProductId });
            builder.Entity<ComboDetail>().HasKey(cd => new { cd.ComboId, cd.ServiceId });
            builder.Entity<TechnicianService>().HasKey(ts => new { ts.EmployeeId, ts.ServiceId });
            builder.Entity<IdentityUserRole<string>>().HasKey(r => new { r.UserId, r.RoleId });
            
            builder.Entity<Employee>().HasIndex(e => e.IdentityUserId).IsUnique();
            builder.Entity<Voucher>().HasIndex(v => v.Code).IsUnique();

            builder.Entity<Invoice>()
                .HasOne(i => i.Appointment)
                .WithOne(a => a.Invoice)
                .HasForeignKey<Invoice>(i => i.AppointmentId);

            builder.Entity<AppointmentDetail>()
                .HasOne(ad => ad.Appointment)
                .WithMany(a => a.AppointmentDetails)
                .HasForeignKey(ad => ad.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppointmentDetail>()
                .HasOne(ad => ad.Technician)
                .WithMany(e => e.ServiceAppointments)
                .HasForeignKey(ad => ad.TechnicianId)
                .OnDelete(DeleteBehavior.NoAction);

            // --- CẤU HÌNH XÓA MỀM TỰ ĐỘNG (SOFT DELETE) ---
            // Duyệt qua tất cả các bảng trong Model
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // Bỏ qua bảng ActivityLog và các bảng hệ thống Identity nếu không cần thiết
                if (typeof(ActivityLog).IsAssignableFrom(entityType.ClrType)) continue;

                // 1. Thêm thuộc tính ẩn (Shadow Property) "IsDeleted"
                // Nếu class chưa có property này, EF sẽ tự thêm vào model
                var isDeletedProperty = entityType.FindProperty("IsDeleted");
                if (isDeletedProperty == null)
                {
                    builder.Entity(entityType.ClrType).Property<bool>("IsDeleted").HasDefaultValue(false);
                }

                // 2. Thêm Bộ lọc truy vấn toàn cục (Global Query Filter)
                // Tự động thêm điều kiện "WHERE IsDeleted = false" vào mọi câu truy vấn
                var method = SetGlobalQueryFilterMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }

        // Helper Method để tạo Global Query Filter thông qua Reflection
        static readonly MethodInfo SetGlobalQueryFilterMethod = typeof(ApplicationDbContext)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(t => t.IsGenericMethod && t.Name == "SetGlobalQueryFilter");

        private void SetGlobalQueryFilter<T>(ModelBuilder builder) where T : class
        {
            builder.Entity<T>().HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }

        // --- LOGIC GHI LOG & XÓA MỀM ---
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            // Bước 1: Tính toán thay đổi
            var auditEntries = OnBeforeSaveChanges();

            try
            {
                // Bước 2: Lưu dữ liệu chính
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

                // Bước 3: Lưu Log
                await OnAfterSaveChanges(auditEntries);

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "Unknown";
            var ip = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

            foreach (var entry in ChangeTracker.Entries())
            {
                // Phòng thủ: Sửa lỗi thiếu dữ liệu cho ActivityLog
                if (entry.Entity is ActivityLog log && entry.State == EntityState.Added)
                {
                    if (string.IsNullOrEmpty(log.AffectedColumns)) log.AffectedColumns = "[]";
                    if (string.IsNullOrEmpty(log.OldValues)) log.OldValues = "{}";
                    if (string.IsNullOrEmpty(log.NewValues)) log.NewValues = "{}";
                    if (string.IsNullOrEmpty(log.EntityId)) log.EntityId = "{}";
                    if (string.IsNullOrEmpty(log.Action)) log.Action = "System";

                    if (string.IsNullOrEmpty(log.IpAddress)) log.IpAddress = ip;
                    continue; 
                }

                if (entry.Entity is ActivityLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                auditEntry.UserId = user;
                auditEntry.IpAddress = ip;
                
                // Xác định loại hành động
                switch (entry.State)
                {
                    case EntityState.Added: auditEntry.AuditType = "Create"; break;
                    case EntityState.Deleted: auditEntry.AuditType = "Delete"; break;
                    case EntityState.Modified: auditEntry.AuditType = "Update"; break;
                }

                // Lấy tên hiển thị
                var nameProp = entry.Properties.FirstOrDefault(p => 
                    p.Metadata.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) || 
                    p.Metadata.Name.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
                    p.Metadata.Name.Equals("FullName", StringComparison.OrdinalIgnoreCase) ||
                    p.Metadata.Name.Equals("Code", StringComparison.OrdinalIgnoreCase));

                if (nameProp != null)
                {
                    var val = entry.State == EntityState.Deleted ? nameProp.OriginalValue : nameProp.CurrentValue;
                    auditEntry.DisplayName = val?.ToString();
                }
                
                // Thu thập dữ liệu các cột
                // LƯU Ý: Thực hiện việc này TRƯỚC khi chuyển đổi trạng thái Xóa Mềm
                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary)
                    {
                        auditEntry.TemporaryProperties.Add(property);
                        continue;
                    }

                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;
                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }

                // --- LOGIC XÓA MỀM (SOFT DELETE) ---
                // Nếu đang là lệnh Xóa (Deleted), chuyển thành Sửa (Modified) và set IsDeleted = true
                if (entry.State == EntityState.Deleted)
                {
                    // Kiểm tra xem bảng này có cột IsDeleted không (Shadow Property hoặc Property thật)
                    var isDeletedProp = entry.Metadata.FindProperty("IsDeleted");
                    if (isDeletedProp != null && isDeletedProp.ClrType == typeof(bool))
                    {
                        // 1. Chuyển trạng thái sang Modified
                        entry.State = EntityState.Modified;
                        
                        // 2. Cập nhật giá trị IsDeleted = true
                        entry.CurrentValues["IsDeleted"] = true;

                        // 3. Vẫn giữ AuditType là "Delete" để log hiển thị đúng ý nghĩa hành động
                        // (Mặc dù về kỹ thuật là Update, nhưng về nghiệp vụ là Xóa)
                    }
                }

                auditEntries.Add(auditEntry);
            }

            // Loại bỏ các bản ghi Update rỗng (không có gì thay đổi)
            // Lưu ý: Soft Delete giờ là Modified nhưng có thay đổi cột IsDeleted, nên sẽ không bị lọc mất
            foreach (var auditEntry in auditEntries.Where(e => !e.HasTemporaryProperties))
            {
                if (auditEntry.AuditType == "Update" && auditEntry.ChangedColumns.Count == 0)
                    auditEntries.Remove(auditEntry);
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var auditEntry in auditEntries)
            {
                foreach (var prop in auditEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                        auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    else
                        auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            foreach (var auditEntry in auditEntries)
            {
                var log = auditEntry.ToAudit();

                // Phòng thủ lớp 2
                if (string.IsNullOrEmpty(log.AffectedColumns)) log.AffectedColumns = "[]";
                if (string.IsNullOrEmpty(log.OldValues)) log.OldValues = "{}";
                if (string.IsNullOrEmpty(log.NewValues)) log.NewValues = "{}";
                if (string.IsNullOrEmpty(log.EntityId)) log.EntityId = "{}";
                if (string.IsNullOrEmpty(log.Action)) log.Action = "System";
                if (string.IsNullOrEmpty(log.IpAddress)) log.IpAddress = "Unknown";

                // Xử lý Description chi tiết (Foreign Key Lookup)
                var entityType = auditEntry.Entry.Metadata;
                var foreignKeys = entityType.GetForeignKeys();
                List<string> relatedInfo = new List<string>();

                foreach (var fk in foreignKeys)
                {
                    var fkProperty = fk.Properties.FirstOrDefault();
                    if (fkProperty == null) continue;

                    string fkPropName = fkProperty.Name;
                    object fkValue = null;

                    if (auditEntry.NewValues.ContainsKey(fkPropName)) fkValue = auditEntry.NewValues[fkPropName];
                    else if (auditEntry.KeyValues.ContainsKey(fkPropName)) fkValue = auditEntry.KeyValues[fkPropName];
                    else if (auditEntry.AuditType == "Delete" && auditEntry.OldValues.ContainsKey(fkPropName)) fkValue = auditEntry.OldValues[fkPropName];

                    if (fkValue != null)
                    {
                        var principalEntityType = fk.PrincipalEntityType.ClrType;
                        string principalName = await GetEntityDisplayNameAsync(principalEntityType, fkValue);

                        if (!string.IsNullOrEmpty(principalName))
                        {
                            string label = fkPropName.EndsWith("Id") ? fkPropName.Substring(0, fkPropName.Length - 2) : fkPropName;
                            relatedInfo.Add($"{label}: {principalName}");
                        }
                    }
                }

                if (relatedInfo.Count > 0)
                {
                    string details = string.Join(", ", relatedInfo);
                    if (!string.IsNullOrEmpty(auditEntry.DisplayName))
                        log.Description = $"{auditEntry.AuditType} {auditEntry.TableName}: {auditEntry.DisplayName} ({details})";
                    else
                        log.Description = $"{auditEntry.AuditType} {auditEntry.TableName}: {details}";
                }
                
                ActivityLogs.Add(log);
            }

            await base.SaveChangesAsync();
        }

        private async Task<string> GetEntityDisplayNameAsync(Type entityType, object id)
        {
            if (id == null) return null;
            try 
            {
                var entity = await this.FindAsync(entityType, id);
                if (entity == null) return null;

                var props = entityType.GetProperties();
                var nameProp = props.FirstOrDefault(p => 
                    p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) || 
                    p.Name.Equals("Title", StringComparison.OrdinalIgnoreCase) || 
                    p.Name.Equals("FullName", StringComparison.OrdinalIgnoreCase) || 
                    p.Name.Equals("Code", StringComparison.OrdinalIgnoreCase));

                return nameProp?.GetValue(entity)?.ToString();
            }
            catch { return null; }
        }
    }

    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public string TableName { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public List<PropertyEntry> TemporaryProperties { get; } = new List<PropertyEntry>();
        public List<string> ChangedColumns { get; } = new List<string>();
        public string AuditType { get; set; }
        public string DisplayName { get; set; } 
        public bool HasTemporaryProperties => TemporaryProperties.Any();

        public ActivityLog ToAudit()
        {
            var audit = new ActivityLog();
            audit.UserId = UserId ?? "Anonymous"; 
            audit.IpAddress = IpAddress ?? "Unknown";
            audit.Action = AuditType ?? "System";
            audit.EntityName = TableName;
            audit.Timestamp = DateTime.Now;
            
            audit.EntityId = KeyValues.Count == 0 ? "{}" : JsonConvert.SerializeObject(KeyValues);
            audit.OldValues = OldValues.Count == 0 ? "{}" : JsonConvert.SerializeObject(OldValues);
            audit.NewValues = NewValues.Count == 0 ? "{}" : JsonConvert.SerializeObject(NewValues);
            audit.AffectedColumns = ChangedColumns.Count == 0 ? "[]" : JsonConvert.SerializeObject(ChangedColumns);
            
            if (!string.IsNullOrEmpty(DisplayName))
                audit.Description = $"{AuditType} {TableName}: {DisplayName}";
            else
                audit.Description = $"{AuditType} record in {TableName}";

            return audit;
        }
    }
}