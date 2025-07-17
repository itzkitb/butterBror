using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Services.External
{
    public class TelegramService
    {
        [Obsolete("Will be rewritten.")]
        public static async Task<long> Ping()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(1);
            var stopwatch = new Stopwatch();
            string url = "https://telegram.org/";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                stopwatch.Start();
                var response = await client.SendAsync(request);
                stopwatch.Stop();

                return stopwatch.ElapsedMilliseconds / 2;
            }
            catch
            {
                return -1;
            }
        }
    }
}
