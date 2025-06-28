using System;
using System.Net.Http;
using System.Diagnostics;

class Program
{
    static async Task Main()
    {
        await Task.Delay(1000);

        var client = new HttpClient();
        var stopwatch = new Stopwatch();
        string url = "https://telegram.org/robots.txt";

        try
        {
            stopwatch.Start();
            var response = await client.GetAsync(url);
            stopwatch.Stop();

            Console.WriteLine($"Время ответа {url} — {stopwatch.ElapsedMilliseconds} мс");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}