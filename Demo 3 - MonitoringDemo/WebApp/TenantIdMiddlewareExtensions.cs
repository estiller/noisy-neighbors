using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace WebApp
{
    public static class TenantIdMiddlewareExtensions
    {
        private const string TenantIdContextItemName = "TenantId";

        public static IApplicationBuilder UseTenantId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantIdMiddleware>();
        }

        public static string GetTenantId(this HttpContext httpContext)
        {
            return (string) httpContext.Items[TenantIdContextItemName];
        }

        public static void SetTenantId(this HttpContext httpContext, string tenantId)
        {
            httpContext.Items[TenantIdContextItemName] = tenantId;
        }
    }
}