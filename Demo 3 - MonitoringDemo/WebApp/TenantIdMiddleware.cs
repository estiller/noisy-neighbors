using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace WebApp
{
    public class TenantIdMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            string authorizationHeader = context.Request.Headers[HeaderNames.Authorization];
            if (authorizationHeader == null)
            {
                return _next(context);
            }

            var authrizationHeaderValue = AuthenticationHeaderValue.Parse(authorizationHeader);
            if (authrizationHeaderValue.Scheme == "tenant")
            {
                context .SetTenantId(authrizationHeaderValue.Parameter);
            }

            return _next(context);
        }
    }
}