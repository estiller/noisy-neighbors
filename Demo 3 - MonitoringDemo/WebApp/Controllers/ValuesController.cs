using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static readonly Random Random = new Random();

        [HttpGet]
        public async Task<string> GetAsync()
        {
            string tenantId = HttpContext.GetTenantId() ?? "No Tenant";

            if (BadTenantId(tenantId))
                await PunishAsync();

            return tenantId;
        }

        private bool BadTenantId(string tenantId)
        {
            return tenantId == "3" || tenantId == "5";
        }

        private Task PunishAsync()
        {
            var delayMs = GetDelayMs();
            return Task.Delay(delayMs);
        }

        private static int GetDelayMs()
        {
            int delayMs;
            lock (Random)
            {
                delayMs = Random.Next(500);
            }
            return delayMs;
        }
    }
}
