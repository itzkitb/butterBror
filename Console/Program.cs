using butterBror;
using butterBror.Utils.Bot;
using butterBror.Utils.Tools.Device;
using DankDB;
using Pastel;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using static butterBror.Utils.Bot.Console;

class Programm
{
    private static readonly string Host = $"http://*:8080";
    private static readonly HttpListener Listener = new();
    private static readonly List<StreamWriter> Clients = new();
    private static readonly object ClientsLock = new();
    private static NetworkInterface selectedInterface;
    private static PerformanceCounter diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
    private static PerformanceCounter diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
    private static Timer timer;
    private static readonly string AdminPasswordHash = "6FF8E2CF58249F757ECEE669C6CB015A1C1F44552442B364C8A388B0BDB1322A7AF6B67678D9206378D8969FFEC48263C9AB3167D222C80486FC848099535568";

    public static void SelectEthernetAdapter()
    {
        System.Console.WriteLine("Select network interface:");

        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

        if (adapters.Length == 0)
        {
            System.Console.WriteLine("No network interfaces available.");
            return;
        }

        var activeAdapters = new List<NetworkInterface>();
        foreach (var adapter in adapters)
        {
            if (adapter.OperationalStatus == OperationalStatus.Up &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                activeAdapters.Add(adapter);
            }
        }

        if (activeAdapters.Count == 0)
        {
            System.Console.WriteLine("There are no active network interfaces..");
            System.Console.ReadLine();
            Environment.Exit(0);
        }

        for (int i = 0; i < activeAdapters.Count; i++)
        {
            var adapter = activeAdapters[i];
            System.Console.WriteLine($"{i + 1}. {adapter.Name} ({adapter.Description})");
        }

        System.Console.Write("Enter interface number: ");
        if (!int.TryParse(System.Console.ReadLine(), out int selectedIndex) ||
            selectedIndex < 1 ||
            selectedIndex > activeAdapters.Count)
        {
            System.Console.WriteLine("Invalid input.");
            SelectEthernetAdapter();
        }
        System.Console.Clear();
        System.Console.Title = $"butterBror | Loading...";
        System.Console.WriteLine("Loading libraries...");
        selectedInterface = activeAdapters[selectedIndex - 1];
    }

    public static async Task Main(string[] args)
    {
        SelectEthernetAdapter();
        
        var botTask = Task.Run(() =>
        {
            OnChatLine += OnChatLineGetted;
            ErrorOccured += OnErrorOccured;
            Core.Start();
        });

        System.Console.Title = $"butterBror | {Core.Version}.{Core.Patch}";
        System.Console.Clear();

        Listener.Prefixes.Add(Host + "/");
        Listener.Start();
        System.Console.WriteLine($"The web interface is running on {Host}");

        timer = new Timer(_ =>
        {
            IPv4InterfaceStatistics ethernetStats = selectedInterface.GetIPv4Statistics();
            double networkReceived = ethernetStats.BytesReceived / 1024 / 1024;
            double networkSent = ethernetStats.BytesSent / 1024 / 1024;
            int diskRead = (int)(diskReadCounter.NextValue() / 1024);
            int diskWrite = (int)(diskWriteCounter.NextValue() / 1024);

            var stats = new
            {
                RestartedTimes = Core.RestartedTimes,
                CompletedCommands = Core.CompletedCommands,
                Users = Core.Users,
                Version = $"{Core.Version}.{Core.Patch}",
                Coins = Core.Coins,
                IsTwitchReconnected = Core.Bot.TwitchReconnected,
                IsInitialized = Core.Bot.Initialized,
                IsConnected = Core.Bot.Connected,
                Name = Core.Bot.BotName,
                MessagesProcessed = Core.Bot.MessagesProccessed,
                Battery = $"{Battery.GetBatteryCharge()}% ({Battery.IsCharging()})",
                Memory = $"{Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)} Mbyte",
                WorkTime = $"{DateTime.Now - Core.StartTime:dd\\:hh\\:mm\\.ss}",
                CPU = $"{(int)Core.CPUPercentage}%",
                Ticks = Core.TicksCounter,
                SkippedTicks = Core.SkippedTicks,
                CacheItems = Worker.cache.count,
                Emotes = Core.Bot.EmotesCache.Count,
                SevenTV = Core.Bot.ChannelsSevenTVEmotes.Count,
                EmoteSets = Core.Bot.EmoteSetsCache.Count,
                SevenTVUSC = Core.Bot.UsersSearchCache.Count,
                Currency = Core.Coins == 0 ? 0 : Core.BankDollars / Core.Coins,
                NetworkReceived = networkReceived,
                NetworkSend = networkSent,
                DiskRead = diskRead,
                DiskWrite = diskWrite
            };
            BroadcastEvent("stats", stats);
        }, null, 0, 1000);

        while (true)
        {
            var context = await Listener.GetContextAsync();
            _ = HandleRequestAsync(context);
        }
    }

    private static void OnChatLineGetted(LineInfo line)
    {
        if (!new string[] { "tw_chat", "ds_chat", "tg_chat" }.Contains(line.Channel))
        {
            System.Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.FF").PadRight(11).Pastel("#666666")} [ {line.Channel.Pastel("#ff7b42")} ] {line.Message.Pastel("#bababa")}");
            var logData = new
            {
                Time = DateTime.UtcNow.ToString("HH:mm:ss.FF"),
                Channel = line.Channel,
                Message = line.Message,
                Level = line.Level
            };
            BroadcastEvent("log", logData);
        }
    }

    private static void OnErrorOccured(LineInfo line)
    {
        System.Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.FF").PadRight(11).Pastel("#666666")} [ {line.Channel.Pastel("#ff4f4f")} ] {line.Message.Pastel("#bababa")}");
        var logData = new
        {
            Time = DateTime.UtcNow.ToString("HH:mm:ss.FF"),
            Channel = line.Channel,
            Message = line.Message,
            Level = line.Level
        };
        BroadcastEvent("log", logData);
    }

    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.Url?.AbsolutePath == "/")
        {
            response.ContentType = "text/html; charset=utf-8";
            var html = GetHtmlPage();
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            return;
        }

        if (request.Url?.AbsolutePath == "/events")
        {
            string providedPassword = request.QueryString["pass"];

            if (!VerifyPassword(providedPassword))
            {
                response.StatusCode = 401;
                response.StatusDescription = "Unauthorized";
                response.Close();
                return;
            }

            response.ContentType = "text/event-stream";
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var writer = new StreamWriter(response.OutputStream, Encoding.UTF8);
            try
            {
                lock (ClientsLock)
                {
                    Clients.Add(writer);
                }
                while (true)
                {
                    await Task.Delay(15000);
                    await writer.WriteAsync(": keep-alive\n\n");
                    await writer.FlushAsync();
                }
            }
            catch
            {
                lock (ClientsLock)
                {
                    Clients.Remove(writer);
                }
                writer.Dispose();
            }
            return;
        }

        response.StatusCode = 404;
        response.Close();
    }

    private static bool VerifyPassword(string inputPassword)
    {
        string result;
        var bytes = Encoding.UTF8.GetBytes(inputPassword);
        using (var hash = SHA512.Create())
        {
            var hashedInputBytes = hash.ComputeHash(bytes);
            var hashedInputStringBuilder = new StringBuilder(128);
            foreach (var b in hashedInputBytes)
                hashedInputStringBuilder.Append(b.ToString("X2"));
            result = hashedInputStringBuilder.ToString();
        }

        /*
        Console.WriteLine(inputPassword);
        Console.WriteLine(result);
        Console.WriteLine(AdminPasswordHash);
        */
        return result == AdminPasswordHash;
    }

    private static string GetHtmlPage()
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>butterBror Dashboard</title>
    <style>
        :root {{
            --bg-color: #121212;
            --card-bg: #1e1e1e;
            --card-bg-transparent: #1e1e1e70;
            --text-color: #e0e0e0;
            --accent-color: #cff280;
            --log-info: #cff280;
            --log-warning: #ff7b42;
            --log-error: #ff4f4f;
            --log-time: #8a8a8a;
            --scroll-thumb: #3a3a3a;
        }}

        body {{
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: var(--bg-color);
            color: var(--text-color);
            display: flex;
            flex-direction: column;
            min-height: 100vh;
        }}

        header {{
            padding: 1.5rem;
            background: var(--card-bg);
            text-align: center;
            border: 1px solid #ffffff20;
            border-bottom-left-radius: 20px;
            border-bottom-right-radius: 20px;
        }}

        h1 {{
            margin: 0;
            font-size: 2.5rem;
            color: var(--accent-color);
            letter-spacing: 1px;
        }}

        main {{
            flex: 1;
            display: grid;
            grid-template-columns: 2fr 1fr;
            gap: 2rem;
            padding: 2rem;
            max-width: 1400px;
            margin: 0 auto;
        }}

        #logs {{
            background-color: var(--card-bg);
            border-radius: 20px;
            padding: 1rem;
            height: 70vh;
            overflow-y: auto;
            position: relative;
            background-image: url(""https://cdn.7tv.app/emote/01H16FA16G0005EZED5J0EY7KN/4x.webp"");
            background-repeat: no-repeat;
            background-position-y: 100%;
            background-size: 400px;
            border: 1px solid #ffffff20;
        }}

        .log-line {{
            margin-top: 0.5rem;
            padding: 0.5rem;
            font-size: 0.95rem;
            display: flex;
            align-items: center;
            border: 1px solid #ffffff20;
            border-radius: 10px;
            backdrop-filter: blur(20px);
            background-color: var(--card-bg-transparent);
        }}

        .log-channel {{
            color: var(--accent-color);
            margin-right: 1rem;
            min-width: 50px;
        }}
    
        .INFO .log-channel {{
            color: var(--log-info);
        }}

        .WARNING .log-channel {{
            color: var(--log-warning);
        }}

        .log-time {{
            color: var(--log-time);
            margin-right: 1rem;
            min-width: 85px;
        }}

        #logs::-webkit-scrollbar {{
            width: 20px;
        }}

        #logs::-webkit-scrollbar-track {{
            background: #00000000;
        }}

        #logs::-webkit-scrollbar-thumb {{
            background-color: var(--scroll-thumb);
            border-top-right-radius: 20px;
            border-bottom-right-radius: 20px;
        }}

        #stats {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 1.5rem;
            flex-direction: column;
        }}

        .stat-card {{
            background-color: var(--card-bg);
            border-radius: 20px;
            transition: transform 0.2s ease;
            border: 1px solid #ffffff20;
            padding-bottom: 1.5rem;
        }}

        .stat-title {{
            color: var(--accent-color);
            padding: 1.5rem;
            font-size: 1.1rem;
            margin-bottom: 0.8rem;
            border-bottom: 1px solid #ffffff20;
            padding-bottom: 0.5rem;
        }}

        .stat-value {{
            font-size: 1.2rem;
            margin-top: 0.5rem;
            padding-left: 1.5rem;
            padding-right: 1.5rem;
        }}

        @media (max-width: 1000px) {{
            main {{
                grid-template-columns: 1fr;
            }}
            
            #stats {{
                grid-template-columns: repeat(1, 1fr);
            }}
        }}

        @media (max-width: 600px) {{
            h1 {{
                font-size: 2rem;
            }}
        }}
    </style>
</head>
<body>
    <header>
        <h1>ButterBror Dashboard</h1>
    </header>
    
    <main>
        <div id=""logs""></div>
        
        <div id=""stats""></div>
    </main>

    <script>
        function connectToEvents() {{
            const password = prompt('Enter the password to access the panel:');
            if (!password) {{
                alert('Password is required for access');
                return connectToEvents();
            }}

            const encodedPassword = encodeURIComponent(password);
            const eventSource = new EventSource(`/events?pass=${{encodedPassword}}`);

            eventSource.addEventListener('log', function(event) {{
                const data = JSON.parse(event.data);
                const logDiv = document.getElementById('logs');
            
                const levelClass = data.Level || 'INFO';
            
                logDiv.innerHTML += `
                    <div class=""log-line ${{levelClass}}"">
                        <div class=""log-time"">[${{data.Time}}]</div>
                        <div class=""log-channel"">[${{data.Channel}}]</div>
                        <div>${{data.Message}}</div>
                    </div>
                `;
            
                logDiv.scrollTop = logDiv.scrollHeight;
            }});

            eventSource.addEventListener('stats', function(event) {{
            const stats = JSON.parse(event.data);
            const statsContainer = document.getElementById('stats');
            
            statsContainer.innerHTML = `
                <div class=""stat-card"">
                    <div class=""stat-title"">Status</div>
                    <div class=""stat-value"">Restarts: ${{stats.RestartedTimes}}</div>
                    <div class=""stat-value"">Commands executed: ${{stats.CompletedCommands}}</div>
                    <div class=""stat-value"">Users: ${{stats.Users}}</div>
                    <div class=""stat-value"">Connected: ${{stats.IsConnected ? '✅ Yes' : '❌ No'}}</div>
                    <div class=""stat-value"">Initialized: ${{stats.IsInitialized ? '✅ Yes' : '❌ No'}}</div>
                    <div class=""stat-value"">Twitch: ${{stats.IsTwitchReconnected ? '🔄 Reconnected' : '✅ Connected'}}</div>
                </div>

                <div class=""stat-card"">
                    <div class=""stat-title"">Performance</div>
                    <div class=""stat-value"">Messages: ${{stats.MessagesProcessed}}</div>
                    <div class=""stat-value"">Ticks: ${{stats.Ticks}}</div>
                    <div class=""stat-value"">Skipped ticks: ${{stats.SkippedTicks}}</div>
                    <div class=""stat-value"">Work time: ${{stats.WorkTime}}</div>
                </div>

                <div class=""stat-card"">
                    <div class=""stat-title"">Resources</div>
                    <div class=""stat-value"">Memory: ${{stats.Memory}}</div>
                    <div class=""stat-value"">CPU: ${{stats.CPU}}</div>
                    <div class=""stat-value"">Battery: ${{stats.Battery}}</div>
                    <div class=""stat-value"">Downloaded: ${{stats.NetworkReceived}} MByte</div>
                    <div class=""stat-value"">Sended: ${{stats.NetworkSend}} MByte</div>
                    <div class=""stat-value"">Write: ${{stats.DiskWrite}} KB/s</div>
                    <div class=""stat-value"">Read: ${{stats.DiskRead}} KB/s</div>
                </div>

                <div class=""stat-card"">
                    <div class=""stat-title"">Cache</div>
                    <div class=""stat-value"">DankDB: ${{stats.CacheItems}}</div>
                    <div class=""stat-value"">Emotes: ${{stats.Emotes}}</div>
                    <div class=""stat-value"">7TV: ${{stats.SevenTV}}</div>
                    <div class=""stat-value"">Emote sets: ${{stats.EmoteSets}}</div>
                    <div class=""stat-value"">7TV Users: ${{stats.SevenTVUSC}}</div>
                </div>

                <div class=""stat-card"">
                    <div class=""stat-title"">System</div>
                    <div class=""stat-value"">Version: ${{stats.Version}}</div>
                    <div class=""stat-value"">Coins: ${{stats.Coins}}</div>
                    <div class=""stat-value"">Course: ${{stats.Currency}}$</div>
                    <div class=""stat-value"">Bot name: ${{stats.Name}}</div>
                </div>
            `;
            }});
        }}

        window.onload = connectToEvents;
    </script>
</body>
</html>";
    }

    private static async void BroadcastEvent(string eventName, object data)
    {
        var json = $"event: {eventName}\ndata: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}\n\n";
        List<StreamWriter> disconnectedClients = new();

        List<StreamWriter> clientsCopy;
        lock (ClientsLock)
        {
            clientsCopy = Clients.ToList();
        }

        foreach (var client in clientsCopy)
        {
            try
            {
                await client.WriteAsync(json);
                await client.FlushAsync();
            }
            catch
            {
                disconnectedClients.Add(client);
            }
        }

        foreach (var client in disconnectedClients)
        {
            lock (ClientsLock)
            {
                Clients.Remove(client);
            }
            client.Dispose();
        }
    }
}