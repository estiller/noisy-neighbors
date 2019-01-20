using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        private const int MaxTenantId = 10;
        private const int ClientCount = 50;

        public static void Main()
        {
            Console.WriteLine("Press <ENTER> to start");
            Console.ReadLine();

            var cts = new CancellationTokenSource();
            List<Task> clientTasks = new List<Task>();
            for (int i = 0; i < ClientCount; i++)
            {
                var clientTask = SendRequestsAsync(cts.Token);
                clientTasks.Add(clientTask);
            }

            Console.WriteLine("Press <ENTER> to finish");
            Console.ReadLine();
            cts.Cancel();

            try
            {
                Task.WaitAll(clientTasks.ToArray());
            }
            catch (AggregateException)
            {
            }
        }

        private static async Task SendRequestsAsync(CancellationToken cancellationToken)
        {
            var random = new Random();
            var client = new HttpClient();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tenantId = random.Next(MaxTenantId).ToString(CultureInfo.InvariantCulture);
                    var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:50617/api/values");
                    request.Headers.Authorization = new AuthenticationHeaderValue("tenant", tenantId);
                    await client.SendAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An exception occured while sending a request: {ex}");
                }
            }
        }
    }
}