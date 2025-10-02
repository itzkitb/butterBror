using System.Diagnostics;
using Telegram.Bot;

namespace bb.Services.External
{
    public class TelegramPing
    {
        public static async Task<long> Ping()
        {
            var stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();
                var response = bb.Program.BotInstance.Clients.Telegram.GetMe().Result;
                stopwatch.Stop();

                return stopwatch.ElapsedMilliseconds;
            }
            catch
            {
                return -1;
            }
        }
    }
}
