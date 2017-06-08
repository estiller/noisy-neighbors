using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace WebApp
{
    public class TenantIdTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantIdTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            string tenantId = _httpContextAccessor.HttpContext?.GetTenantId();
            if (tenantId != null)
            {
                telemetry.Context.Properties["TenantId"] = tenantId;
            }
        }
    }
}