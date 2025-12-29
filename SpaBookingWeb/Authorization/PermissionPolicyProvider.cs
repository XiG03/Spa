using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace SpaBookingWeb.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            // Dùng cấu hình mặc định cho các policy không phải là Permission custom
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() 
            => FallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() 
            => FallbackPolicyProvider.GetFallbackPolicyAsync();

        // ĐÂY LÀ PHẦN QUAN TRỌNG NHẤT
        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            // Khi code gọi [Authorize(Policy = "Permissions.Products.View")]
            // Hàm này sẽ kiểm tra xem policyName có bắt đầu bằng "Permissions" không
            if (policyName.StartsWith("Permissions", StringComparison.OrdinalIgnoreCase))
            {
                // Tự động tạo ra một Policy mới với Requirement tương ứng
                var policy = new AuthorizationPolicyBuilder();
                policy.AddRequirements(new PermissionRequirement(policyName));
                return Task.FromResult(policy.Build());
            }

            // Nếu không phải (ví dụ Policy="AdminOnly"), trả về xử lý mặc định
            return FallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}