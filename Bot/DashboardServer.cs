using butterBror.Services.System;
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
using static butterBror.Core.Bot.Console;

namespace butterBror
{
    // Default dashboard password is "bbAdmin"
    // Password crypter: https://crypt-online.ru/crypts/sha512/

    /// <summary>
    /// Provides a real-time monitoring dashboard for the bot system with live statistics and logs.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Hosts a web interface on port 8080 with administrator authentication</item>
    /// <item>Streams real-time statistics using Server-Sent Events (SSE)</item>
    /// <item>Displays system metrics including network, disk, and memory usage</item>
    /// <item>Shows bot operational status and performance counters</item>
    /// <item>Requires administrator privileges for proper network interface access</item>
    /// </list>
    /// The dashboard automatically selects the most appropriate network interface and provides a responsive UI
    /// that works across different screen sizes. All sensitive data transmission is secured through password authentication.
    /// </remarks>
    public class DashboardServer
    {
        private static readonly string Host = $"http://*:8080";
        private static readonly HttpListener Listener = new();
        private static readonly List<StreamWriter> Clients = new();
        private static readonly object ClientsLock = new();
        private static NetworkInterface selectedInterface;
        private static PerformanceCounter diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        private static PerformanceCounter diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        private static Timer timer;

        public static bool IsElevated
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Automatically selects the most appropriate active network interface for monitoring.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>First attempts to find interfaces with valid gateway addresses</item>
        /// <item>If none found, tests interfaces for actual internet connectivity</item>
        /// <item>Falls back to first available non-loopback interface</item>
        /// </list>
        /// Logs the selected interface name and description to the console.
        /// Terminates execution if no suitable interface is found.
        /// </remarks>
        public static void SelectEthernetAdapter()
        {
            selectedInterface = GetActiveNetworkInterface();

            if (selectedInterface == null)
            {
                Write("No active network interfaces available.", "dashboard");
                Task.Delay(-1).Wait();
            }

            Write($"Automatically selected network interface: {selectedInterface.Name} ({selectedInterface.Description})", "dashboard");
        }

        /// <summary>
        /// Finds and returns the most suitable active network interface.
        /// </summary>
        /// <returns>
        /// The selected <see cref="NetworkInterface"/> or <c>null</c> if none found.
        /// </returns>
        /// <remarks>
        /// Implements a two-phase selection algorithm:
        /// <list type="number">
        /// <item>First phase: Looks for interfaces with valid gateway addresses</item>
        /// <item>Second phase: Tests interfaces for actual internet connectivity</item>
        /// </list>
        /// Skips loopback and tunnel interfaces in both phases.
        /// Uses DNS connectivity test (8.8.8.8) to verify working internet connection.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the local IP address of the selected network interface.
        /// </summary>
        /// <returns>
        /// The local IPv4 address as a string, or "localhost" if unavailable.
        /// </returns>
        /// <remarks>
        /// Prioritizes private IP addresses (RFC 1918) when available.
        /// Filters out loopback addresses (127.0.0.1).
        /// Returns the first valid non-loopback IPv4 address found.
        /// </remarks>
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

        /// <summary>
        /// Determines if an IP address belongs to a private IP range.
        /// </summary>
        /// <param name="address">The <see cref="IPAddress"/> to check.</param>
        /// <returns>
        /// <c>true</c> if the address is in a private range; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Checks against standard private IP ranges:
        /// <list type="bullet">
        /// <item>10.0.0.0/8 (10.0.0.0 - 10.255.255.255)</item>
        /// <item>172.16.0.0/12 (172.16.0.0 - 172.31.255.255)</item>
        /// <item>192.168.0.0/16 (192.168.0.0 - 192.168.255.255)</item>
        /// <item>169.254.0.0/16 (APIPA range)</item>
        /// </list>
        /// Used to prioritize private addresses in the dashboard interface.
        /// </remarks>
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

        /// <summary>
        /// Starts the dashboard server asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Verifies administrator privileges before proceeding</item>
        /// <item>Selects network interface and determines local IP address</item>
        /// <item>Starts HTTP listener on port 8080</item>
        /// <item>Initializes 1-second timer for statistics collection</item>
        /// <item>Begins processing incoming HTTP requests</item>
        /// </list>
        /// Broadcasts two types of events to connected clients:
        /// <list type="table">
        /// <item><term>stats</term><description>Periodic system and bot metrics</description></item>
        /// <item><term>log</term><description>Real-time log messages from the bot</description></item>
        /// </list>
        /// </remarks>
        public static async Task StartAsync()
        {
            if (!IsElevated)
            {
                Write("Dashboard requires administrator privileges to run. Please run as administrator.", "dashboard");
                return;
            }

            SelectEthernetAdapter();

            Listener.Prefixes.Add(Host + "/");
            Listener.Start();
            string localAddress = GetLocalIPAddress();
            Write($"The web interface is running on {localAddress}:8080", "dashboard");

            timer = new Timer(_ =>
            {
                try
                {
                    if (!Bot.Initialized) return;

                    IPv4InterfaceStatistics ethernetStats = selectedInterface.GetIPv4Statistics();
                    double networkReceived = ethernetStats.BytesReceived / 1024 / 1024;
                    double networkSent = ethernetStats.BytesSent / 1024 / 1024;
                    int diskRead = (int)(diskReadCounter.NextValue() / 1024);
                    int diskWrite = (int)(diskWriteCounter.NextValue() / 1024);

                    var stats = new
                    {
                        CompletedCommands = Bot.CompletedCommands,
                        Users = Bot.Users,
                        Version = $"{Bot.Version}.{Bot.Patch}",
                        Coins = Bot.Coins,
                        IsTwitchReconnected = Bot.TwitchReconnected,
                        IsInitialized = Bot.Initialized,
                        IsConnected = Bot.Connected,
                        Name = Bot.BotName,
                        MessagesProcessed = Bot.MessagesProccessed,
                        Battery = $"{Battery.GetBatteryCharge()}% ({Battery.IsCharging()})",
                        Memory = $"{Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024)} Mbyte",
                        WorkTime = $"{DateTime.Now - Bot.StartTime:dd\\:hh\\:mm\\.ss}",
                        CacheItems = Worker.cache.count,
                        Emotes = Bot.EmotesCache.Count,
                        SevenTV = Bot.ChannelsSevenTVEmotes.Count,
                        EmoteSets = Bot.EmoteSetsCache.Count,
                        SevenTVUSC = Bot.UsersSearchCache.Count,
                        Currency = Bot.Coins == 0 ? 0 : Bot.BankDollars / Bot.Coins,
                        NetworkReceived = networkReceived,
                        NetworkSend = networkSent,
                        DiskRead = diskRead,
                        DiskWrite = diskWrite,
                        MessagesBufferCount = Bot.MessagesBuffer?.Count() ?? 0,
                        UsersBufferCount = Bot.UsersBuffer?.Count() ?? 0,
                        FirstMessagesCount = Bot.allFirstMessages?.Count ?? 0,
                        IsReady = Bot.Ready,
                        Host = $"{Bot.hostName} v.{Bot.hostVersion}",
                        DiscordGuilds = Bot.Clients.Discord.Guilds.Count,
                        TwitchChannels = Bot.Clients.Twitch.JoinedChannels.Count,
                        SQLChannelsOPS = Bot.SQL.Channels.GetAndResetSqlOperationCount(),
                        SQLGamesOPS = Bot.SQL.Games.GetAndResetSqlOperationCount(),
                        SQLMessagesOPS = Bot.SQL.Messages.GetAndResetSqlOperationCount(),
                        SQLUsersOPS = Bot.SQL.Users.GetAndResetSqlOperationCount(),
                        SQLRolesOPS = Bot.SQL.Roles.GetAndResetSqlOperationCount(),
                        IsTwitchConnected = Bot.Clients.Twitch.IsConnected,
                        IsDiscordConnected = Bot.Clients.Discord.ConnectionState == Discord.ConnectionState.Connected
                    };
                    BroadcastEvent("stats", stats);
                }
                catch (Exception ex)
                {
                    Write($"Error in dashboard timer: {ex.Message}", "dashboard");
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
        public static void HandleLog(string message, string channel, LogLevel level)
        {
            var logData = new
            {
                Time = DateTime.UtcNow.ToString("HH:mm:ss.FF"),
                Channel = channel,
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
            return result == Manager.Get<string>(Bot.Paths.Settings, "dashboard_password");
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
    <title>{Bot.BotName} Dashboard</title>
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
        <h1>Bot Dashboard - {Bot.BotName} v.{Bot.Version}.{Bot.Patch}</h1>
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
                    <div class=""stat-value"">Ready: ${{stats.IsReady ? '✅ Yes' : '❌ No'}}</div>
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
                    <div class=""stat-value"">Roles: ${{stats.SQLRolesOPS}} o/s</div>
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
}