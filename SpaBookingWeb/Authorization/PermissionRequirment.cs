using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace SpaBookingWeb.Authorization
{
    // 1. Tạo một "Yêu cầu" (Requirement) chứa tên quyền cần kiểm tra
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    // 2. Tạo bộ xử lý (Handler) để kiểm tra logic
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Nếu user chưa đăng nhập thì từ chối ngay
            if (context.User == null)
            {
                return Task.CompletedTask;
            }

            // Kiểm tra xem trong danh sách Claims của User có chứa cái quyền (requirement.Permission) hay không
            // Lưu ý: "Permission" là tên loại Claim ta lưu trong DB
            var hasPermission = context.User.Claims.Any(x => x.Type == "Permission" && x.Value == requirement.Permission);
            
            if (hasPermission)
            {
                // Nếu có quyền, đóng dấu CHẤP NHẬN (Succeed)
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}