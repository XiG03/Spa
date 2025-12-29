using System.Collections.Generic;
using System.Linq;

namespace SpaBookingWeb.Constants
{
    public static class Permissions
    {
        // 1. Chỉ cần khai báo tên Module (Tên bảng/Chức năng) ở đây
        // Khi bạn có bảng mới, chỉ cần thêm tên vào danh sách này là xong.
        public static readonly List<string> Modules = new List<string>
        {
            "Products",
            "Customers",
            "Orders",
            "Services",    // Ví dụ thêm mới
            "Bookings",    // Ví dụ thêm mới
            "Employees",   // Ví dụ thêm mới
            "Reports"
        };

        // 2. Các hành động cơ bản (CRUD)
        public static class Actions
        {
            public const string View = "View";
            public const string Create = "Create";
            public const string Edit = "Edit";
            public const string Delete = "Delete";
        }

        // 3. Hàm tự động sinh tất cả quyền dựa trên danh sách Modules
        public static List<string> GetAllPermissions()
        {
            var allPermissions = new List<string>();

            foreach (var module in Modules)
            {
                allPermissions.Add($"Permissions.{module}.{Actions.View}");
                allPermissions.Add($"Permissions.{module}.Create");
                allPermissions.Add($"Permissions.{module}.Edit");
                allPermissions.Add($"Permissions.{module}.Delete");
            }

            // Thêm các quyền đặc biệt (không theo quy tắc CRUD) nếu cần
            allPermissions.Add("Permissions.Settings.Manage");

            return allPermissions;
        }

        // Helper để lấy tên Module từ string permission (Dùng cho View)
        // VD: "Permissions.Products.View" -> Trả về "Products"
        public static string GetModuleFromClaim(string claimValue)
        {
            var parts = claimValue.Split('.');
            return parts.Length > 1 ? parts[1] : "Other";
        }
    }
}