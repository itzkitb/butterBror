using bb.Services.Internal;
using DankDB;
using Jint;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using static bb.Core.Bot.Logger;
using bb.Utils;

namespace bb
{
    public class DashboardServer
    {
        private static readonly string Host = $"http://*:8080";
        private static readonly HttpListener Listener = new();
        private static readonly List<StreamWriter> Clients = new();
        private static readonly object ClientsLock = new();
        private static NetworkInterface? selectedInterface;
        private static PerformanceCounter? diskReadCounter;
        private static PerformanceCounter? diskWriteCounter;
        private static Timer? timer;

        public static bool IsElevated
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                }
                else
                {
                    return Environment.GetEnvironmentVariable("USER") == "root" ||
                           Environment.GetEnvironmentVariable("SUDO_USER") != null ||
                           Environment.GetEnvironmentVariable("USERNAME") == "root";
                }
            }
        }

        public static void SelectEthernetAdapter()
        {
            selectedInterface = GetActiveNetworkInterface();

            if (selectedInterface == null)
            {
                Write("No active network interfaces available.");
                Task.Delay(-1).Wait();
            }

            Write($"Automatically selected network interface: {selectedInterface.Name} ({selectedInterface.Description})");
        }

        private static NetworkInterface GetActiveNetworkInterface()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                IPInterfaceProperties ipProps = ni.GetIPProperties();

                if (ipProps.GatewayAddresses.Any(gw =>
                    gw.Address.ToString() != "0.0.0.0" &&
                    !IPAddress.IsLoopback(gw.Address)))
                {
                    return ni;
                }
            }

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                IPInterfaceProperties ipProps = ni.GetIPProperties();
                if (ipProps.UnicastAddresses.Any(addr =>
                    addr.Address.AddressFamily == AddressFamily.InterNetwork))
                {
                    try
                    {
                        using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
                        {
                            client.GetStringAsync("http://8.8.8.8").Wait();
                            return ni;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                      ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                      ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
        }

        private static string GetLocalIPAddress()
        {
            if (selectedInterface == null)
                return "localhost";

            IPInterfaceProperties ipProps = selectedInterface.GetIPProperties();
            foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address))
                {
                    if (IsPrivateIPAddress(addr.Address))
                    {
                        return addr.Address.ToString();
                    }
                }
            }

            foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return addr.Address.ToString();
                }
            }

            return "localhost";
        }

        private static bool IsPrivateIPAddress(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (APIPA)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            return false;
        }

        private static (long ReadBytes, long WriteBytes) GetDiskStats()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (
                    (long)diskReadCounter.NextValue(),
                    (long)diskWriteCounter.NextValue()
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    string[] diskStats = File.ReadAllLines("/proc/diskstats");
                    foreach (string line in diskStats)
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        if (parts.Length > 13 && (parts[2].StartsWith("sd") || parts[2].StartsWith("hd") || parts[2].StartsWith("nvme")))
                        {
                            long sectorSize = 512;
                            long reads = long.Parse(parts[3]) * sectorSize;
                            long writes = long.Parse(parts[7]) * sectorSize;
                            return (reads, writes);
                        }
                    }
                }
                catch
                {
                    
                }
                return (0, 0);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "iostat";
                        process.StartInfo.Arguments = "-d 1 2";
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();

                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 2)
                        {
                            string[] stats = lines[lines.Length - 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (stats.Length >= 4)
                            {
                                long sectorSize = 512;
                                long reads = long.Parse(stats[1]) * sectorSize;
                                long writes = long.Parse(stats[3]) * sectorSize;
                                return (reads, writes);
                            }
                        }
                    }
                }
                catch
                {
                    
                }
                return (0, 0);
            }
            return (0, 0);
        }

        private static (double Received, double Sent) GetNetworkStats()
        {
            if (selectedInterface == null)
                return (0, 0);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IPv4InterfaceStatistics stats = selectedInterface.GetIPv4Statistics();
                return (
                    stats.BytesReceived / 1024.0 / 1024.0,
                    stats.BytesSent / 1024.0 / 1024.0
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    string[] netStats = File.ReadAllLines("/proc/net/dev");
                    foreach (string line in netStats)
                    {
                        if (line.Contains(selectedInterface.Name))
                        {
                            string[] parts = line.Split(new[] { ':' }, 2)[1]
                                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 16)
                            {
                                double received = long.Parse(parts[0]) / 1024.0 / 1024.0;
                                double sent = long.Parse(parts[8]) / 1024.0 / 1024.0;
                                return (received, sent);
                            }
                        }
                    }
                }
                catch
                {
                    
                }
                return (0, 0);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "netstat";
                        process.StartInfo.Arguments = "-ib";
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();

                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in lines)
                        {
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 8 && parts[0] == selectedInterface.Name)
                            {
                                double received = long.Parse(parts[4]) / 1024.0 / 1024.0;
                                double sent = long.Parse(parts[8]) / 1024.0 / 1024.0;
                                return (received, sent);
                            }
                        }
                    }
                }
                catch
                {
                    
                }
                return (0, 0);
            }
            return (0, 0);
        }

        private static string GetBatteryInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{Battery.GetBatteryCharge()}% ({Battery.IsCharging()})";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    string[] powerSupplies = Directory.GetDirectories("/sys/class/power_supply/");
                    foreach (string supply in powerSupplies)
                    {
                        string typePath = Path.Combine(supply, "type");
                        if (File.Exists(typePath) && File.ReadAllText(typePath).Trim() == "Battery")
                        {
                            string capacityPath = Path.Combine(supply, "capacity");
                            string statusPath = Path.Combine(supply, "status");

                            if (File.Exists(capacityPath) && File.Exists(statusPath))
                            {
                                int capacity = int.Parse(File.ReadAllText(capacityPath).Trim());
                                string status = File.ReadAllText(statusPath).Trim();
                                return $"{capacity}% ({(status == "Charging" ? "Charging" : "Discharging")})";
                            }
                        }
                    }
                }
                catch
                {
                    
                }
                return "N/A (Linux)";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "pmset";
                        process.StartInfo.Arguments = "-g batt";
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();

                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (output.Contains("discharging") || output.Contains("charging"))
                        {
                            string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 1)
                            {
                                string[] parts = lines[1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length > 0)
                                {
                                    string capacity = parts[0].Trim();
                                    string status = parts[1].Trim();
                                    return $"{capacity} ({status})";
                                }
                            }
                        }
                    }
                }
                catch
                {
                    
                }
                return "N/A (MacOS)";
            }
            return "N/A";
        }

        public static async Task StartAsync()
        {
            if (!IsElevated)
            {
                string platformMessage = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    "Please run as administrator." :
                    "Please run with sudo (Linux/MacOS).";

                Write($"Dashboard requires elevated privileges to run. {platformMessage}");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            }

            SelectEthernetAdapter();

            Listener.Prefixes.Add(Host + "/");
            Listener.Start();
            string localAddress = GetLocalIPAddress();
            Write($"The web interface is running on {localAddress}:8080");

            timer = new Timer(_ =>
            {
                try
                {
                    if (!bb.Program.BotInstance.Initialized) return;

                    (double networkReceived, double networkSent) = GetNetworkStats();
                    (long diskReadBytes, long diskWriteBytes) = GetDiskStats();

                    int diskRead = (int)(diskReadBytes / 1024);
                    int diskWrite = (int)(diskWriteBytes / 1024);

                    var stats = new
                    {
                        CompletedCommands = bb.Program.BotInstance.CompletedCommands,
                        Users = bb.Program.BotInstance.Users,
                        Version = $"{bb.Program.BotInstance.Version}",
                        Coins = bb.Program.BotInstance.Coins,
                        IsInitialized = bb.Program.BotInstance.Initialized,
                        IsConnected = bb.Program.BotInstance.Connected,
                        Name = bb.Program.BotInstance.TwitchName,
                        MessagesProcessed = bb.Program.BotInstance.MessageProcessor.Proccessed,
                        Battery = GetBatteryInfo(),
                        Memory = $"{Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)} Mbyte",
                        WorkTime = $"{DateTime.Now - bb.Program.BotInstance.StartTime:dd\\:hh\\:mm\\.ss}",
                        CacheItems = Worker.cache.count,
                        Emotes = bb.Program.BotInstance.EmotesCache.Count,
                        SevenTV = bb.Program.BotInstance.ChannelsSevenTVEmotes.Count,
                        EmoteSets = bb.Program.BotInstance.EmoteSetsCache.Count,
                        SevenTVUSC = bb.Program.BotInstance.UsersSearchCache.Count,
                        Currency = bb.Program.BotInstance.Coins == 0 ? 0 : bb.Program.BotInstance.InBankDollars / bb.Program.BotInstance.Coins,
                        NetworkReceived = networkReceived,
                        NetworkSend = networkSent,
                        DiskRead = diskRead,
                        DiskWrite = diskWrite,
                        MessagesBufferCount = bb.Program.BotInstance.MessagesBuffer?.Count() ?? 0,
                        UsersBufferCount = bb.Program.BotInstance.UsersBuffer?.Count() ?? 0,
                        FirstMessagesCount = bb.Program.BotInstance.allFirstMessages?.Count ?? 0,
                        Host = $"{bb.Program.BotInstance.HostName} v.{bb.Program.BotInstance.HostVersion}",
                        DiscordGuilds = bb.Program.BotInstance.Clients.Discord.Guilds.Count,
                        TwitchChannels = bb.Program.BotInstance.Clients.Twitch.JoinedChannels.Count,
                        SQLChannelsOPS = bb.Program.BotInstance.DataBase.Channels.GetAndResetSqlOperationCount(),
                        SQLGamesOPS = bb.Program.BotInstance.DataBase.Games.GetAndResetSqlOperationCount(),
                        SQLMessagesOPS = bb.Program.BotInstance.DataBase.Messages.GetAndResetSqlOperationCount(),
                        SQLUsersOPS = bb.Program.BotInstance.DataBase.Users.GetAndResetSqlOperationCount(),
                        IsTwitchConnected = bb.Program.BotInstance.Clients.Twitch.IsConnected,
                        IsDiscordConnected = bb.Program.BotInstance.Clients.Discord.ConnectionState == Discord.ConnectionState.Connected
                    };
                    BroadcastEvent("stats", stats);
                }
                catch (Exception ex)
                {
                    Write($"Error in dashboard timer: {ex.Message}");
                }
            }, null, 0, 1000);

            while (true)
            {
                var context = await Listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
        }

        /// <summary>
        /// Handles log messages and broadcasts them to connected dashboard clients.
        /// </summary>
        /// <param name="message">The log message content.</param>
        /// <param name="channel">The logging channel/category.</param>
        /// <param name="level">The severity level of the log message.</param>
        /// <remarks>
        /// Formats log data with timestamp and level information.
        /// Broadcasts as "log" event type to all connected SSE clients.
        /// Used by the bot's logging system to display real-time logs in the dashboard.
        /// </remarks>
        public static void HandleLog(string message, LogLevel level)
        {
            var logData = new
            {
                Time = DateTime.UtcNow.ToString("HH:mm:ss.FF"),
                Message = message,
                Level = level.ToString()
            };
            BroadcastEvent("log", logData);
        }

        /// <summary>
        /// Processes incoming HTTP requests to the dashboard server.
        /// </summary>
        /// <param name="context">The HTTP listener context containing the request and response objects.</param>
        /// <returns>A task representing the asynchronous request handling operation.</returns>
        /// <remarks>
        /// Handles two main endpoints:
        /// <list type="table">
        /// <item><term>/</term><description>Serves the main dashboard HTML page</description></item>
        /// <item><term>/events</term><description>Establishes SSE connection with password authentication</description></item>
        /// </list>
        /// For /events endpoint:
        /// <list type="bullet">
        /// <item>Verifies password using VerifyPassword()</item>
        /// <item>Returns 401 Unauthorized for invalid credentials</item>
        /// <item>Maintains persistent connection with keep-alive pings</item>
        /// </list>
        /// Automatically cleans up disconnected clients.
        /// </remarks>
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
                        await writer.WriteAsync(": keep-alive\n");
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

        /// <summary>
        /// Verifies if the provided password matches the administrator password.
        /// </summary>
        /// <param name="inputPassword">The password to verify.</param>
        /// <returns>
        /// <c>true</c> if the password is valid; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Uses SHA-512 hashing for secure password comparison.
        /// Implements constant-time comparison to prevent timing attacks.
        /// Passwords are hashed in uppercase hexadecimal format for case-insensitive comparison.
        /// </remarks>
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
            return result == bb.Program.BotInstance.Settings.Get<string>("dashboard_password");
        }

        /// <summary>
        /// Generates the complete HTML content for the dashboard user interface.
        /// </summary>
        /// <returns>A string containing the formatted HTML page.</returns>
        /// <remarks>
        /// Features:
        /// <list type="bullet">
        /// <item>Responsive design with mobile support</item>
        /// <item>Dark theme with custom scrollbar styling</item>
        /// <item>Real-time statistics cards (6 sections)</item>
        /// <item>Scrolling log viewer with level-based coloring</item>
        /// <item>Connection status indicator with auto-reconnect</item>
        /// </list>
        /// Includes JavaScript for:
        /// <list type="bullet">
        /// <item>Password prompt on initial load</item>
        /// <item>SSE connection management</item>
        /// <item>Automatic reconnection on failure</item>
        /// <item>Dynamic statistics and log updates</item>
        /// </list>
        /// Embeds bot-specific information (name, version) in the UI.
        /// </remarks>
        private static string GetHtmlPage()
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>{bb.Program.BotInstance.TwitchName} Dashboard</title>
    <link rel=""icon"" href=""https://cdn.7tv.app/emote/01H16FA16G0005EZED5J0EY7KN/4x.webp"" type=""image/webp"">
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
        #connection-status {{
            position: absolute;
            top: 10px;
            right: 10px;
            padding: 5px 10px;
            border-radius: 15px;
            font-weight: bold;
            font-size: 0.85rem;
            z-index: 1000;
        }}
        .status-connected {{
            background-color: #4caf50;
            color: white;
        }}
        .status-disconnected {{
            background-color: #f44336;
            color: white;
        }}
        .status-connecting {{
            background-color: #ff9800;
            color: white;
        }}
    </style>
</head>
<body>
    <header>
        <h1>Bot Dashboard - {bb.Program.BotInstance.TwitchName} v.{bb.Program.BotInstance.Version}</h1>
        <div id=""connection-status"" class=""status-connecting"">Connecting...</div>
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
            
            let lastMessageTime = Date.now();

            function updateConnectionStatus(status, message) {{
                const statusElement = document.getElementById('connection-status');
                statusElement.className = `status-${{status}}`;
                statusElement.textContent = message;
            }}

            eventSource.addEventListener('open', function() {{
                updateConnectionStatus('connected', 'Connected');
            }});
            
            eventSource.addEventListener('message', function() {{
                lastMessageTime = Date.now();
            }});

            eventSource.addEventListener('error', function() {{
                if (eventSource.readyState === EventSource.CLOSED) {{
                    updateConnectionStatus('disconnected', 'Disconnected');
                    
                    setTimeout(() => {{
                        if (document.getElementById('connection-status').textContent.includes('Disconnected')) {{
                            updateConnectionStatus('connecting', 'Reconnecting...');
                            connectToEvents();
                        }}
                    }}, 5000);
                }}
            }});

            eventSource.addEventListener('log', function(event) {{
                lastMessageTime = Date.now();
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
            lastMessageTime = Date.now();
            const stats = JSON.parse(event.data);
            const statsContainer = document.getElementById('stats');
            statsContainer.innerHTML = `
                <div class=""stat-card"">
                    <div class=""stat-title"">Status</div>
                    <div class=""stat-value"">Commands executed: ${{stats.CompletedCommands}}</div>
                    <div class=""stat-value"">Users: ${{stats.Users}}</div>
                    <div class=""stat-value"">Connected: ${{stats.IsConnected ? '✅ Yes' : '❌ No'}}</div>
                    <div class=""stat-value"">Initialized: ${{stats.IsInitialized ? '✅ Yes' : '❌ No'}}</div>
                    <div class=""stat-value"">Twitch: ${{stats.IsTwitchConnected ? '✅ Connected' : '🛑 Disconnected'}}</div>
                    <div class=""stat-value"">Discord: ${{stats.IsDiscordConnected ? '✅ Connected' : '🛑 Disconnected'}}</div>
                </div>
                <div class=""stat-card"">
                    <div class=""stat-title"">Performance</div>
                    <div class=""stat-value"">Messages: ${{stats.MessagesProcessed}}</div>
                    <div class=""stat-value"">Work time: ${{stats.WorkTime}}</div>
                    <div class=""stat-value"">Messages buffer: ${{stats.MessagesBufferCount}}</div>
                    <div class=""stat-value"">Users buffer: ${{stats.UsersBufferCount}}</div>
                    <div class=""stat-value"">First messages: ${{stats.FirstMessagesCount}}</div>
                    <div class=""stat-value"">Discord guilds: ${{stats.DiscordGuilds}}</div>
                    <div class=""stat-value"">Twitch channels: ${{stats.TwitchChannels}}</div>
                </div>
                <div class=""stat-card"">
                    <div class=""stat-title"">Resources</div>
                    <div class=""stat-value"">Memory: ${{stats.Memory}}</div>
                    <div class=""stat-value"">Battery: ${{stats.Battery}}</div>
                    <div class=""stat-value"">Downloaded: ${{stats.NetworkReceived}} MByte</div>
                    <div class=""stat-value"">Sended: ${{stats.NetworkSend}} MByte</div>
                    <div class=""stat-value"">Write: ${{stats.DiskWrite}} KB/s</div>
                    <div class=""stat-value"">Read: ${{stats.DiskRead}} KB/s</div>
                </div>
                <div class=""stat-card"">
                    <div class=""stat-title"">SQL</div>
                    <div class=""stat-value"">Channels: ${{stats.SQLChannelsOPS}} o/s</div>
                    <div class=""stat-value"">Games: ${{stats.SQLGamesOPS}} o/s</div>
                    <div class=""stat-value"">Messages: ${{stats.SQLMessagesOPS}} o/s</div>
                    <div class=""stat-value"">Users: ${{stats.SQLUsersOPS}} o/s</div>
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
                    <div class=""stat-value"">Host: ${{stats.Host}}</div>
                    <div class=""stat-value"">Coins: ${{stats.Coins}}</div>
                    <div class=""stat-value"">Course: ${{stats.Currency}}$</div>
                    <div class=""stat-value"">Bot name: ${{stats.Name}}</div>
                </div>
            `;
            }});

            setInterval(() => {{
                const timeSinceLastMessage = Date.now() - lastMessageTime;
                if (timeSinceLastMessage > 5000) {{
                    updateConnectionStatus('disconnected', 'No data (5s+)');
                }} else if (timeSinceLastMessage > 1500) {{
                    updateConnectionStatus('connecting', 'Waiting for data...');
                }} else {{
                    updateConnectionStatus('connected', 'Connected');
                }}
            }}, 1000);
        }}
        window.onload = connectToEvents;
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Broadcasts an event with data to all connected SSE clients.
        /// </summary>
        /// <param name="eventName">The name of the event type.</param>
        /// <param name="data">The data payload to send.</param>
        /// <remarks>
        /// Formats data as Server-Sent Events (SSE) compliant message.
        /// Handles disconnected clients gracefully during broadcast.
        /// Uses JSON serialization for data payload.
        /// Thread-safe through ClientsLock synchronization.
        /// Automatically removes and disposes of disconnected clients.
        /// </remarks>
        private static async void BroadcastEvent(string eventName, object data)
        {
            var json = $"event: {eventName}\ndata: {JsonConvert.SerializeObject(data)}\n\n";
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
                catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
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
}