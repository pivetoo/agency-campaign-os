using System.Security.Claims;
using AgencyCampaign.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AgencyCampaign.Infrastructure.Security
{
    public sealed class PermissionChecker : IPermissionChecker
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public PermissionChecker(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public bool IsRoot => User?.HasClaim("root", "true") ?? false;

        public bool HasPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return false;
            }

            if (IsRoot)
            {
                return true;
            }

            return User?.HasClaim("permission", permission) ?? false;
        }

        public bool HasAny(params string[] permissions)
        {
            if (permissions is null || permissions.Length == 0)
            {
                return false;
            }

            if (IsRoot)
            {
                return true;
            }

            foreach (string permission in permissions)
            {
                if (User?.HasClaim("permission", permission) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
