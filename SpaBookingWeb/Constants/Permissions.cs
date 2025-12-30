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
            "Reports",
            "BlogPosts"
        };

        public static class Products
        {
            public const string View = "Permissions.Products.View";
            public const string Create = "Permissions.Products.Create";
            public const string Edit = "Permissions.Products.Edit";
            public const string Delete = "Permissions.Products.Delete";
        }

        public static class Customers
        {
            public const string View = "Permissions.Customers.View";
            public const string Create = "Permissions.Customers.Create";
            public const string Edit = "Permissions.Customers.Edit";
            public const string Delete = "Permissions.Customers.Delete";
        }

        public static class Orders
        {
            public const string View = "Permissions.Orders.View";
            public const string Create = "Permissions.Orders.Create";
            public const string Edit = "Permissions.Orders.Edit";
            public const string Delete = "Permissions.Orders.Delete";
        }

        public static class Services
        {
            public const string View = "Permissions.Services.View";
            public const string Create = "Permissions.Services.Create";
            public const string Edit = "Permissions.Services.Edit";
            public const string Delete = "Permissions.Services.Delete";
        }

        public static class Bookings
        {
            public const string View = "Permissions.Bookings.View";
            public const string Create = "Permissions.Bookings.Create";
            public const string Edit = "Permissions.Bookings.Edit";
            public const string Delete = "Permissions.Bookings.Delete";
        }

        public static class Employees
        {
            public const string View = "Permissions.Employees.View";
            public const string Create = "Permissions.Employees.Create";
            public const string Edit = "Permissions.Employees.Edit";
            public const string Delete = "Permissions.Employees.Delete";
        }

        public static class Reports
        {
            public const string View = "Permissions.Reports.View";
            public const string Create = "Permissions.Reports.Create";
            public const string Edit = "Permissions.Reports.Edit";
            public const string Delete = "Permissions.Reports.Delete";
        }

        public static class BlogPosts
        {
            public const string View = "Permissions.BlogPosts.View";
            public const string Create = "Permissions.BlogPosts.Create";
            public const string Edit = "Permissions.BlogPosts.Edit";
            public const string Delete = "Permissions.BlogPosts.Delete";
        }

        public static class Settings
        {
            public const string Manage = "Permissions.Settings.Manage";
        }

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