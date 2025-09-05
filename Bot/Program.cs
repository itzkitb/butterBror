﻿using bb.Core.Bot;
using bb.Core.Services;
using bb.Data;
using bb.Events;
using bb.Models;
using bb.Models.DataBase;
using bb.Services.External;
using bb.Services.System;
using bb.Utils;
using bb.Workers;
using DankDB;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Pastel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;
using static bb.Core.Bot.Console;

namespace bb
{
    /// <summary>
    /// Central class.
    /// </summary>
    public class Bot
    {
        #region Variables
        #region Core
        public static Version Version = new Version("2.18.0.2");
        public static DateTime StartTime = new();
        public static string PreviousVersion = "";
        public static bool Initialized = false;
        public static bool Connected => Clients.Twitch?.IsConnected == true && Clients.Discord?.ConnectionState == Discord.ConnectionState.Connected;
        public static string? BotName;
        public static char DefaultExecutor = '#';
        private static bool SkipFetch = false;
        #endregion Core

        #region Currency
        public static int Users = 0;
        public static int InBankDollars = 0;
        public static int CompletedCommands = 0;
        public static decimal Coins = 0;
        public static int CurrencyMentioned = 8;
        public static int CurrencyMentioner = 2;
        public static string CoinSymbol = "BTR";
        #endregion Currency

        #region Hosting
        public static string HostName = null;
        public static string HostVersion = null;
        #endregion Hosting

        #region Data
        public static ClientService Clients = new ClientService();
        public static PathService Paths = new PathService();
        internal static Tokens Tokens = new Tokens();
        public static SQLService? DataBase;
        #endregion Data

        #region Buffers
        public static MessagesBuffer? MessagesBuffer;
        public static UsersBuffer? UsersBuffer;
        public static List<(PlatformsEnum platform, string channelId, long userId, Message message)> allFirstMessages = new();
        #endregion

        #region Twitch
        public static string? TwitchClientId;
        public static string[] TwitchNewVersionAnnounce = [];
        public static string[] TwitchReconnectAnnounce = [];
        public static string[] TwitchConnectAnnounce = [];
        #endregion Twitch

        #region Cache
        public static ConcurrentDictionary<string, (SevenTV.Types.Rest.Emote emote, DateTime expiration)> EmotesCache = new();
        public static ConcurrentDictionary<string, (List<string> emotes, DateTime expiration)> ChannelsSevenTVEmotes = new();
        public static ConcurrentDictionary<string, (string userId, DateTime expiration)> UsersSearchCache = new();
        public static ConcurrentDictionary<string, (string setId, DateTime expiration)> EmoteSetsCache = new();
        public static Dictionary<string, string> UsersSevenTVIDs = new Dictionary<string, string>();
        public static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(30);
        #endregion Cache


        public static SevenTvService SevenTvService = new SevenTvService(new HttpClient());
        public static IServiceProvider? DiscordServiceProvider;
        public static ReceiverOptions? TelegramReceiverOptions;
        public static CommandService? DiscordCommandService;
        
        private static DateTime _startTime = DateTime.UtcNow;
        private static CpuUsage _CpuUsage = new CpuUsage();
        private static Task? _repeater;

        private static DateTime _lastTelemtry = DateTime.UtcNow.AddMinutes(-1);
        private static DateTime _lastSave = DateTime.UtcNow.AddMinutes(-1);
        #endregion
        #region Core
        /// <summary>
        /// Entry point of the application. Initializes and starts the bot core.
        /// </summary>
        /// <param name="args">Command-line arguments. Supported parameters:
        /// <list type="table">
        /// <item><term>--core-name [host_name]</term><description> Specifies the host name for execution</description></item>
        /// <item><term>--core-version [version]</term><description> Specifies the host version</description></item>
        /// </list>
        /// </param>
        public static void Main(string[] args)
        {
            System.Console.Title = "Loading libraries...";
            System.Console.WriteLine(":alienPls: Loading libraries...");

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            Initialize(args);

            if (SkipFetch)
            {
                Write("Launched with --skip-fetch parameter, skipping system fetch...", "core");
            }
            else
            {
                SystemDataFetch();
            }

            Setup();
            StartBot();

            Task.Run(async () =>
            {
                try
                {
                    await DashboardServer.StartAsync();
                }
                catch (Exception ex)
                {
                    Write($"Error starting dashboard: {ex.Message}", "dashboard", LogLevel.Error);
                }
            });

            System.Console.ReadLine();
        }

        /// <summary>
        /// Background task for periodic system metrics updates and data persistence.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Updates database statistics every second</item>
        /// <item>Tracks currency metrics (coins, dollars, users) with file persistence</item>
        /// <item>Performs semaphore cleanup for inactive command handlers every 10 minutes</item>
        /// <item>Sends telemetry data at the start of each hour (00 seconds)</item>
        /// <item>Executes daily database backups at 00:00 UTC</item>
        /// <item>Persists buffered messages and user data at the start of each minute</item>
        /// </list>
        /// The task automatically adjusts delay to synchronize with system clock (triggers exactly at second boundaries).
        /// Handles all exceptions to prevent task termination while logging errors.
        /// </remarks>
        private static async Task StartRepeater()
        {
            await Task.Delay(1010 - DateTime.UtcNow.Millisecond);

            while (true)
            {
                try
                {
                    if (Initialized)
                    {
                        DateTime now = DateTime.UtcNow;
                        Telemetry.CPUItems++;
                        Telemetry.CPU += (decimal)_CpuUsage.GetUsage();

                        if (now.Minute % 10 == 0 && (now - _lastTelemtry).Minutes > 0)
                        {
                            _lastTelemtry = now;
                            Paths.UpdatePaths();
                            EmoteCacheService.Save();
                            _ = Telemetry.Send();

                            var timeout = TimeSpan.FromMinutes(10);
                            foreach (var (userId, (semaphore, lastUsed)) in MessageProcessor.messagesSemaphores.ToList())
                            {
                                if (now - lastUsed > timeout)
                                {
                                    if (MessageProcessor.messagesSemaphores.TryRemove(userId, out var entry))
                                    {
                                        try
                                        {
                                            if (entry.Semaphore.CurrentCount == 0)
                                                entry.Semaphore.Release();
                                        }
                                        finally
                                        {
                                            entry.Semaphore.Dispose();
                                        }
                                    }
                                }
                            }
                        }

                        if (now.Hour == 0 && now.Minute == 0 && now.Second == 0)
                        {
                            _ = Backup.BackupDataAsync();

                            Write("Reinitializing currency counters...", "core");
                            Users = DataBase.Users.GetTotalUsers();
                            Coins = DataBase.Users.GetTotalBalance();
                        }

                        //Write($"({now.Second == 0} && {(now - _lastSave).TotalSeconds > 2}); ({now.Second}; {(now - _lastSave).TotalSeconds}; {now}; {_lastSave})", "debug");
                        if (now.Second == 0 && (now - _lastSave).TotalSeconds > 2)
                        {
                            _lastSave = now;
                            Stopwatch stopwatch = Stopwatch.StartNew();

                            #region Buffer save 
                            int messages = MessagesBuffer.Count() + allFirstMessages.Count;
                            int users = UsersBuffer.Count();

                            if (MessagesBuffer.Count() > 0)
                            {
                                MessagesBuffer.Flush();
                            }

                            if (UsersBuffer.Count() > 0)
                            {
                                UsersBuffer.Flush();
                            }

                            if (allFirstMessages.Count > 0)
                            {
                                DataBase.Channels.SaveFirstMessages(allFirstMessages);
                                allFirstMessages.Clear();
                            }
                            #endregion Buffer save 
                            #region Currency save
                            Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", Coins },
                                    { "users", Users },
                                    { "dollars", InBankDollars },
                                    { "cost", InBankDollars / Coins },
                                    { "middleBalance", Coins / Users }
                            };

                            if (Paths.Currency is not null)
                            {
                                Manager.Save(Paths.Currency, "total", currencyData);
                                Manager.Save(Paths.Currency, $"{now.Day}.{now.Month}.{now.Year}", currencyData);
                            }
                            #endregion Currency save

                            stopwatch.Stop();
                            Write($"Saved {messages} messages, {users} users and currency in {stopwatch.ElapsedMilliseconds} ms", "info");
                        }
                    }
                }
                catch (Exception e)
                {
                    Write(e);
                }
                finally
                {
                    await Task.Delay(1010 - DateTime.UtcNow.Millisecond);
                }
            }
        }

        /// <summary>
        /// Collects and displays hardware/software environment information.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Processor: Retrieved via WMI (Windows only)</item>
        /// <item>Operating System: Displays OS caption/name</item>
        /// <item>Memory: Total RAM and available space in GB</item>
        /// <item>Disk Space: Aggregate storage and free space across all drives</item>
        /// </list>
        /// Outputs stylized ASCII-art banner with color-highlighted system metrics.
        /// Logs warnings but continues execution when data collection fails.
        /// Uses Pastel library for colored console output where applicable.
        /// </remarks>
        private static void SystemDataFetch()
        {
            Write("Please wait...", "core");
            string processor = "Unnamed processor";
            string OSName = "Unknown";
            double memory = 0;
            long totalDiskSpace = 0;
            long totalFreeDiskSpace = 0;

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                    {
                        foreach (ManagementObject cpu in searcher.Get())
                        {
                            processor = cpu["Name"]?.ToString() ?? "Unnamed processor";
                            break;
                        }
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    try
                    {
                        string cpuInfo = File.ReadAllText("/proc/cpuinfo");
                        string[] lines = cpuInfo.Split('\n');
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("model name"))
                            {
                                processor = line.Split(':')[1].Trim();
                                break;
                            }
                        }
                        if (processor == "Unnamed processor")
                        {
                            processor = "Linux CPU";
                        }
                    }
                    catch
                    {
                        processor = "Linux CPU";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    try
                    {
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = "sysctl";
                            process.StartInfo.Arguments = "-n machdep.cpu.brand_string";
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.Start();

                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            if (!string.IsNullOrEmpty(output))
                            {
                                processor = output.Trim();
                            }
                            else
                            {
                                processor = "Apple CPU";
                            }
                        }
                    }
                    catch
                    {
                        processor = "Apple CPU";
                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Unable to get processor info: {ex.Message}", "core");
            } // CPU

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                        {
                            var os = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                            OSName = os?.GetPropertyValue("Caption")?.ToString() ?? Environment.OSVersion.ToString();
                        }
                    }
                    catch
                    {
                        OSName = Environment.OSVersion.ToString();
                    }
                }
                else
                {
                    OSName = RuntimeInformation.OSDescription;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/etc/os-release"))
                    {
                        try
                        {
                            string osInfo = File.ReadAllText("/etc/os-release");
                            foreach (string line in osInfo.Split('\n'))
                            {
                                if (line.StartsWith("PRETTY_NAME"))
                                {
                                    OSName = line.Split('=')[1].Trim('"');
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Unable to get OS name: {ex.Message}", "core");
                OSName = Environment.OSVersion.ToString();
            } // OS name

            try
            {
                memory = Memory.BytesToGB(Memory.GetTotalMemoryBytes());
            }
            catch (Exception ex)
            {
                Write($"Unable to get RAM size: {ex.Message}", "core");
            } // RAM

            try
            {
                bool drivesFound = false;
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        totalDiskSpace += drive.TotalSize;
                        totalFreeDiskSpace += drive.AvailableFreeSpace;
                        drivesFound = true;
                    }
                }

                if (!drivesFound && (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX)))
                {
                    try
                    {
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = "df";
                            process.StartInfo.Arguments = "-B1";
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.Start();

                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            string[] lines = output.Split('\n');
                            foreach (string line in lines)
                            {
                                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 4 && long.TryParse(parts[1], out long total) &&
                                    long.TryParse(parts[3], out long free))
                                {
                                    if (!parts[0].StartsWith("tmpfs") && !parts[0].StartsWith("dev") &&
                                        !parts[0].StartsWith("sys") && !parts[0].StartsWith("run") &&
                                        !parts[0].StartsWith("none"))
                                    {
                                        totalDiskSpace += total;
                                        totalFreeDiskSpace += free;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Unable to get disks sizes: {ex.Message}", "core");
            } // Drives

            Write($@"

            :::::::::                  
        :::::::::::::::::              
      ::i:::::::::::::::::::           
  :::::::::::::::::::::::::::::        ButterBror
 ::::::::::::::::::::::::::::::::      Host: {HostName} v.{HostVersion}
 {{~:::::::::::::::::::::::::::::::     Framework: {RuntimeInformation.FrameworkDescription.Pastel("#ff7b42")}
 0000XI::::::::::::::::::::::tC00:     v.{Version.ToString().Pastel("#ff7b42")}
 ::c0000nI::::::::::::::::(v1::<l      {OSName.Pastel("#ff7b42")}
 ((((:n0000f-::::::::}}x00(::n000(:     {processor.Pastel("#ff7b42")}
 n0((::::c0000f(:::>}}X(l!00QQ0((::     RAM: {memory.ToString().Pastel("#ff7b42")} GB
  :():::::::C000000000000:::::+l:      Total disks space: {Math.Round(Memory.BytesToGB(totalDiskSpace)).ToString().Pastel("#ff7b42")} GB
     Ix:(((((((:-}}-:((:::100_:         Available disks space: {Math.Round(Memory.BytesToGB(totalFreeDiskSpace)).ToString().Pastel("#ff7b42")} GB
        :X00:((:::::]000x;:            
            :x0000000n:                
              :::::::
", "core");
        }

        /// <summary>
        /// Initializes core bot parameters and environment.
        /// </summary>
        /// <param name="args">Command-line configuration parameters:
        /// <list type="table">
        /// <item><term>--core-name</term><description> Host name (mandatory in RELEASE builds)</description></item>
        /// <item><term>--core-version</term><description> Host version (mandatory in RELEASE builds)</description></item>
        /// </list>
        /// </param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Sets console title with bot version information</item>
        /// <item>Validates critical host parameters in RELEASE configuration</item>
        /// <item>Initializes CPU performance counter for Windows systems</item>
        /// <item>Performs TPS (ticks per second) validation (1-1000 range)</item>
        /// </list>
        /// Terminates execution with error message if required parameters are missing in RELEASE mode.
        /// </remarks>
        private static void Initialize(string[] args)
        {
            System.Console.Title = $"butterBror | v.{Version}";
            System.Console.Clear();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--core-version" && i + 1 < args.Length)
                {
                    HostVersion = args[i + 1];
                }

                if (args[i] == "--core-name" && i + 1 < args.Length)
                {
                    HostName = args[i + 1];
                }

                if (args[i] == "--skip-fetch")
                {
                    SkipFetch = true;
                }
            }

#if RELEASE
            if (HostName is null || HostVersion is null)
            {
                Write("The bot is running without a host! Please run it from the host, not directly.", "core");
                System.Console.ReadLine();
                return;
            }
#endif
        }

        /// <summary>
        /// Configures core infrastructure before bot startup.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Establishes working directories in ApplicationData</item>
        /// <item>Launches background timer task (StartRepeater)</item>
        /// <item>Records application start timestamp</item>
        /// <item>Initializes message and user data buffers</item>
        /// </list>
        /// Executes once during application startup before platform connections.
        /// Configures path structure for all persistent data storage.
        /// </remarks>
        private static void Setup()
        {
            Paths.General = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/ItzKITb/";
            Paths.Main = Paths.General + "butterBror/";

            _repeater = Task.Run(() => StartRepeater());
            Write($"TPS counter successfully started.", "core");

            StartTime = DateTime.Now;
        }

        /// <summary>
        /// Launches the main bot loop and initializes components.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Loads currency statistics from persistent storage</item>
        /// <item>Creates required configuration directories and files</item>
        /// <item>Initializes SQL database connections through SQLService</item>
        /// <item>Authenticates with Twitch using OAuth token</item>
        /// <item>Triggers platform connection sequence</item>
        /// </list>
        /// Records current version in VERSION.txt for update tracking.
        /// Initiates shutdown sequence via Shutdown() on critical failures.
        /// Handles both new installations and version upgrades.
        /// </remarks>
        public static async void StartBot()
        {
            _startTime = DateTime.UtcNow;

            if (FileUtil.FileExists(Paths.Currency))
            {
                var data = Manager.Get<Dictionary<string, dynamic>>(Paths.Currency, "total");
                InBankDollars = data.ContainsKey("dollars") ? data["dollars"].GetInt32() : 0;
            }

            try
            {
                Write("Creating directories...", "initialization");
                string[] directories = { Paths.General, Paths.Main, Paths.TranslateDefault, Paths.TranslateCustom };

                foreach (var dir in directories)
                {
                    FileUtil.CreateDirectory(dir);
                }

                string[] directories_with_platforms = { Paths.TranslateDefault, Paths.TranslateCustom };

                if (!FileUtil.FileExists(Paths.Settings))
                {
                    SettingsService.InitializeFile(Paths.Settings);
                    Write($"The settings file has been created! ({Paths.Settings})", "initialization");
                    Thread.Sleep(-1);
                }

                string[] files = {
                            Paths.BlacklistWords, Paths.BlacklistReplacements,
                            Paths.Currency, Paths.Cache, Paths.Logs, Paths.APIUses,
                            Path.Combine(Paths.TranslateDefault, "ru-RU.json"),
                            Path.Combine(Paths.TranslateDefault, "en-US.json"), Path.Combine(Paths.Main, "VERSION.txt")
                    };

                Write("Creating files...", "initialization");
                foreach (var file in files)
                {
                    FileUtil.CreateFile(file);
                }

                Bot.PreviousVersion = File.ReadAllText(Path.Combine(Paths.Main, "VERSION.txt"));
                File.WriteAllText(Path.Combine(Paths.Main, "VERSION.txt"), $"{Bot.Version}");

                Write("Loading settings...", "initialization");
                SettingsService.Load();

                Write("Initializing databases...", "initialization");
                DataBase = new()
                {
                    Messages = new(Paths.MessagesDatabase),
                    Users = new(Paths.UsersDatabase),
                    Games = new(Paths.GamesDatabase),
                    Channels = new(Paths.ChannelsDatabase),
                    Roles = new(Paths.RolesDatabase)
                };
                MessagesBuffer = new(DataBase.Messages);
                UsersBuffer = new(DataBase.Users);

                Write("Loading currency counters...", "initialization");
                Users = DataBase.Users.GetTotalUsers();
                Coins = DataBase.Users.GetTotalBalance();

                Write("Getting twitch token...", "initialization");
                Tokens.TwitchGetter = new(TwitchClientId, Tokens.TwitchSecretToken, Paths.Main + "TWITCH_AUTH.json");
                var token = await TwitchToken.GetTokenAsync();

                if (token != null)
                {
                    Tokens.Twitch = token;
                    await Connect();
                }
                else
                {
                    Write("Twitch token is null! Something went wrong...", "initialization");
                    await Shutdown();
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                await Shutdown();
            }
        }
        #endregion Core
        #region Connects
        /// <summary>
        /// Establishes connections to all supported platforms.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Twitch: Initializes client with 20+ event handlers</item>
        /// <item>Discord: Registers command services and event subscriptions</item>
        /// <item>Telegram: Starts message polling with error handling</item>
        /// <item>Loads 7TV emote cache from persistent storage</item>
        /// </list>
        /// Sets Ready and Connected flags upon successful initialization.
        /// Measures and logs total initialization time in milliseconds.
        /// Handles all connection exceptions through centralized error handling.
        /// </remarks>
        public static async Task Connect()
        {
            try
            {
                Write("Connecting...", "initialization");

                var tasks = new List<Task>
                {
                    Task.Run(ConnectToTwitch),
                    ConnectToDiscord(),
                    Task.Run(ConnectToTelegram),
                    Task.Run(EmoteCacheService.Load)
                };

                await Task.WhenAll(tasks);

                TimeSpan ConnectedIn = DateTime.UtcNow - _startTime;
                Initialized = true;

                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);

                    if (PreviousVersion != Version.ToString() && PreviousVersion != string.Empty)
                    {
                        foreach (string channel in TwitchNewVersionAnnounce)
                        {
                            PlatformMessageSender.TwitchSend(UsernameResolver.GetUsername(channel, PlatformsEnum.Twitch, true), $"{Bot.BotName} v.{Bot.PreviousVersion} > v.{Bot.Version}", channel, "", "en-US", true);
                        }
                    }

                    foreach (string channel in Bot.TwitchConnectAnnounce)
                    {
                        PlatformMessageSender.TwitchSend(UsernameResolver.GetUsername(channel, PlatformsEnum.Twitch, true), $"{Bot.BotName} Started in {(long)(ConnectedIn).TotalMilliseconds} ms!", channel, "", "en-US", true);
                    }
                });

                Write($"Well done! ({(long)(ConnectedIn).TotalMilliseconds} ms)", "initialization");
            }
            catch (Exception ex)
            {
                Write(ex);
                await Shutdown();
            }
        }

        /// <summary>
        /// Connects to Twitch API and configures event handling.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Authenticates using OAuth token with required scopes</item>
        /// <item>Registers handlers for chat, subscription, and channel events</item>
        /// <item>Joins configured channels and bot's own channel</item>
        /// <item>Sends connection notification message with "truckCrash" emote</item>
        /// </list>
        /// Message rate limits: 750 messages per 30 seconds.
        /// Handles channel ID resolution through Names.GetUsername().
        /// Automatically reconnects on disconnection events.
        /// </remarks>
        private static void ConnectToTwitch()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(BotName, "oauth:" + Tokens.Twitch.AccessToken);
            ClientOptions client_options = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(10)
            };
            WebSocketClient webSocket_client = new WebSocketClient(client_options);
            Clients.Twitch = new TwitchClient(webSocket_client);
            Clients.Twitch.Initialize(credentials, BotName, DefaultExecutor);
            Clients.TwitchAPI = new TwitchAPI();
            Clients.TwitchAPI.Settings.AccessToken = Tokens.Twitch.AccessToken;
            Clients.TwitchAPI.Settings.ClientId = TwitchClientId;
            Clients.TwitchAPI.Settings.Scopes = [AuthScopes.Chat_Read, AuthScopes.Helix_Moderator_Read_Chatters, AuthScopes.Chat_Edit, AuthScopes.Helix_Moderator_Manage_Banned_Users];

            #region Events subscription
            Clients.Twitch.OnJoinedChannel += TwitchEvents.OnJoin;
            Clients.Twitch.OnChatCommandReceived += Core.Commands.Executor.Twitch;
            Clients.Twitch.OnMessageReceived += TwitchEvents.OnMessageReceived;
            Clients.Twitch.OnMessageThrottled += TwitchEvents.OnMessageThrottled;
            Clients.Twitch.OnMessageSent += TwitchEvents.OnMessageSend;
            Clients.Twitch.OnAnnouncement += TwitchEvents.OnAnnounce;
            Clients.Twitch.OnBanned += TwitchEvents.OnBanned;
            Clients.Twitch.OnConnectionError += TwitchEvents.OnConnectionError;
            Clients.Twitch.OnContinuedGiftedSubscription += TwitchEvents.OnContinuedGiftedSubscription;
            Clients.Twitch.OnChatCleared += TwitchEvents.OnChatCleared;
            Clients.Twitch.OnDisconnected += TwitchEvents.OnTwitchDisconnected;
            Clients.Twitch.OnReconnected += TwitchEvents.OnReconnected;
            Clients.Twitch.OnError += TwitchEvents.OnError;
            Clients.Twitch.OnIncorrectLogin += TwitchEvents.OnIncorrectLogin;
            Clients.Twitch.OnLeftChannel += TwitchEvents.OnLeftChannel;
            Clients.Twitch.OnRaidNotification += TwitchEvents.OnRaidNotification;
            Clients.Twitch.OnNewSubscriber += TwitchEvents.OnNewSubscriber;
            Clients.Twitch.OnGiftedSubscription += TwitchEvents.OnGiftedSubscription;
            Clients.Twitch.OnCommunitySubscription += TwitchEvents.OnCommunitySubscription;
            Clients.Twitch.OnReSubscriber += TwitchEvents.OnReSubscriber;
            Clients.Twitch.OnSuspended += TwitchEvents.OnSuspended;
            Clients.Twitch.OnConnected += TwitchEvents.OnConnected;
            Clients.Twitch.OnLog += TwitchEvents.OnLog;
            Clients.Twitch.OnChatCleared += TwitchEvents.OnChatCleared;
            #endregion

            Clients.Twitch.Connect();

            JoinTwitchChannels();

            Clients.Twitch.SendMessage(BotName.ToLower(), "truckCrash Connecting to twitch...");

            Write("Twitch is ready.", "initialization");
        }

        /// <summary>
        /// Connects to Twitch channels specified in the configuration.
        /// Resolves channel usernames to valid channel names and joins each channel.
        /// Logs connection attempts and reports any channels that couldn't be found.
        /// </summary>
        /// <remarks>
        /// The method first attempts to resolve all channel names using UsernameResolver.
        /// It then connects to each valid channel and logs warnings for any channels that couldn't be resolved.
        /// This is typically called during application initialization to establish connections to all configured Twitch channels.
        /// </remarks>
        public static void JoinTwitchChannels()
        {
            var notFoundedChannels = new List<string>();

            foreach (var channel in Manager.Get<string[]>(Paths.Settings, "twitch_connect_channels"))
            {
                var tempChannel = UsernameResolver.GetUsername(channel, PlatformsEnum.Twitch, true);
                if (tempChannel != null) Clients.Twitch.JoinChannel(tempChannel);
                else notFoundedChannels.Add(channel);
            }

            if (notFoundedChannels.Count > 0)
            {
                Write("Twitch - Can't find ID for " + string.Join(',', notFoundedChannels), "core", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Initializes Discord connection with command registration.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Configures client with required GatewayIntents</item>
        /// <item>Registers 10+ event handlers for messages, commands, and guilds</item>
        /// <item>Loads text and slash commands through CommandService</item>
        /// <item>Authenticates and establishes WebSocket connection</item>
        /// </list>
        /// Implements dependency injection through ServiceCollection.
        /// Maintains 1000-message cache for efficient command processing.
        /// Handles both guild and direct message contexts.
        /// </remarks>
        private static async Task ConnectToDiscord()
        {
            var discordConfig = new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent
            };
            Clients.Discord = new DiscordSocketClient(discordConfig);
            DiscordCommandService = new CommandService();
            DiscordServiceProvider = new ServiceCollection()
                .AddSingleton(Clients.Discord)
                .AddSingleton(DiscordCommandService)
                .BuildServiceProvider();

            Clients.Discord.Log += Events.DiscordEvents.LogAsync;
            Clients.Discord.JoinedGuild += Events.DiscordEvents.ConnectToGuilt;
            Clients.Discord.Ready += DiscordWorker.ReadyAsync;
            Clients.Discord.MessageReceived += DiscordWorker.MessageReceivedAsync;
            Clients.Discord.SlashCommandExecuted += Events.DiscordEvents.SlashCommandHandler;
            Clients.Discord.ApplicationCommandCreated += Events.DiscordEvents.ApplicationCommandCreated;
            Clients.Discord.ApplicationCommandDeleted += Events.DiscordEvents.ApplicationCommandDeleted;
            Clients.Discord.ApplicationCommandUpdated += Events.DiscordEvents.ApplicationCommandUpdated;
            Clients.Discord.ChannelCreated += Events.DiscordEvents.ChannelCreated;
            Clients.Discord.ChannelDestroyed += Events.DiscordEvents.ChannelDeleted;
            Clients.Discord.ChannelUpdated += Events.DiscordEvents.ChannelUpdated;
            Clients.Discord.Connected += Events.DiscordEvents.Connected;
            Clients.Discord.ButtonExecuted += Events.DiscordEvents.ButtonTouched;

            await DiscordWorker.RegisterCommandsAsync();
            await Clients.Discord.LoginAsync(TokenType.Bot, Tokens.Discord);
            await Clients.Discord.StartAsync();

            Write("Discord is ready.", "initialization");
        }

        /// <summary>
        /// Starts message reception from Telegram platform.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Initializes client with bot token authentication</item>
        /// <item>Configures update options (text messages only)</item>
        /// <item>Starts asynchronous polling with error handling</item>
        /// <item>Uses CancellationToken for graceful shutdown</item>
        /// </list>
        /// Drops pending updates on startup (DropPendingUpdates=true).
        /// Processes only Message-type updates (UpdateType.Message).
        /// Integrates with TelegramEvents error handling system.
        /// </remarks>
        private static void ConnectToTelegram()
        {
            Clients.Telegram = new TelegramBotClient(Tokens.Telegram);
            TelegramReceiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                DropPendingUpdates = true,
            };

            Clients.Telegram.StartReceiving(Events.TelegramEvents.UpdateHandler, Events.TelegramEvents.ErrorHandler, TelegramReceiverOptions, Clients.TelegramCancellationToken.Token);
            Write("Telegram is ready.", "initialization");
        }
        #endregion Connects
        #region Other
        /// <summary>
        /// Refreshes the Twitch authentication token and reestablishes connection to the Twitch platform.
        /// This method handles the complete process of disconnecting, updating credentials, and reconnecting.
        /// </summary>
        /// <remarks>
        /// The method first disconnects from Twitch if currently connected, then sets new connection credentials
        /// using the updated access token. It then attempts to reconnect and join all configured channels.
        /// Upon successful refresh, it sends notification messages to indicate the token was updated.
        /// If any errors occur during the process, they are logged but not rethrown to prevent application crashes.
        /// This is essential for maintaining uninterrupted connection to Twitch when access tokens expire.
        /// </remarks>
        public static async Task RefreshTwitchTokenAsync()
        {
            try
            {
                if (Clients.Twitch.IsConnected)
                {
                    Clients.Twitch.Disconnect();
                    await Task.Delay(500);
                }

                Clients.Twitch.SetConnectionCredentials(
                    new ConnectionCredentials(BotName, Tokens.Twitch.AccessToken)
                );

                try
                {
                    Clients.Twitch.Connect();
                    JoinTwitchChannels();
                    Write("The token has been updated and the connection has been restored", "core");
                    PlatformMessageSender.TwitchSend(Bot.BotName, $"sillyCatThinks Token refreshed", "", "", "en-US", true);
                }
                catch (Exception ex)
                {
                    Write("Twitch connection error!", "core");
                    Write(ex);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Executes a graceful shutdown sequence with platform-specific notifications and resource cleanup.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Clears initialization flags (<c>Initialized</c>, <c>Connected</c>) immediately</item>
        /// <item>Disposes all buffer systems with null-safety checks</item>
        /// <item>Properly cancels and disposes Telegram polling operations</item>
        /// <item>Gracefully disconnects Discord client with cleanup</item>
        /// <item>Logs each stage of the shutdown sequence for diagnostics</item>
        /// <item>Ensures all pending messages are processed before termination</item>
        /// </list>
        /// <para>
        /// This method follows a structured shutdown sequence:
        /// <list type="number">
        /// <item>Flag reset and resource disposal (immediate)</item>
        /// <item>Buffered data flush with timeout handling</item>
        /// <item>Network connection termination</item>
        /// <item>Final process termination with diagnostic exit code</item>
        /// </list>
        /// </para>
        /// <para>
        /// Critical considerations:
        /// <list type="bullet">
        /// <item>Uses proper async/await pattern without blocking calls</item>
        /// <item>Implements comprehensive error handling for each resource</item>
        /// <item>Ensures all disposables are properly released</item>
        /// <item>Waits for critical operations to complete before exit</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <returns>Task representing the asynchronous shutdown operation</returns>
        public static async Task Shutdown(bool force = false)
        {
            Write("Initiating shutdown sequence...", "core");

            Initialized = false;

            Write($"Shutdown process started (PID: {Environment.ProcessId})", "core", LogLevel.Info);

            try
            {
                if (UsersBuffer != null)
                {
                    try
                    {
                        Write("Flushing user data buffer...", "core");
                        UsersBuffer.Flush();
                        Write("User buffer disposed successfully", "core", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"User buffer flush failed: {ex.Message}", "core", LogLevel.Warning);
                    }
                    finally
                    {
                        UsersBuffer.Dispose();
                        UsersBuffer = null;
                    }
                }

                if (MessagesBuffer != null)
                {
                    try
                    {
                        Write("Flushing message data buffer...", "core");
                        MessagesBuffer.Flush();
                        Write("Message buffer disposed successfully", "core", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Message buffer flush failed: {ex.Message}", "core", LogLevel.Warning);
                    }
                    finally
                    {
                        MessagesBuffer.Dispose();
                        MessagesBuffer = null;
                    }
                }

                if (Clients?.TelegramCancellationToken != null)
                {
                    try
                    {
                        Write("Cancelling Telegram operations...", "core");
                        Clients.TelegramCancellationToken.Cancel();
                        Write("Telegram cancellation requested", "core", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Telegram cancellation failed: {ex.Message}", "core", LogLevel.Warning);
                    }
                    finally
                    {
                        Clients.TelegramCancellationToken.Dispose();
                        Clients.TelegramCancellationToken = null;
                    }
                }

                if (Clients?.Discord != null)
                {
                    try
                    {
                        Write("Disconnecting from Discord...", "core");
                        await Clients.Discord.LogoutAsync();
                        await Clients.Discord.StopAsync();
                        Write("Discord client disconnected", "core", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Discord disconnect failed: {ex.Message}", "core", LogLevel.Warning);
                    }
                    finally
                    {
                        Clients.Discord.Dispose();
                        Clients.Discord = null;
                    }
                }

                if (DataBase != null)
                {
                    Write("Disposing SQL...", "core");
                    try
                    {
                        DataBase.Channels.Dispose();
                        DataBase.Users.Dispose();
                        DataBase.Games.Dispose();
                        DataBase.Messages.Dispose();
                        DataBase.Roles.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Write($"SQL dispose failed: {ex.Message}", "core", LogLevel.Warning);
                    }
                }

                try
                {
                    Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", Coins },
                                    { "users", Users },
                                    { "dollars", InBankDollars },
                                    { "cost", InBankDollars / Coins },
                                    { "middleBalance", Coins / Users }
                            };

                    if (Paths.Currency is not null)
                    {
                        Manager.Save(Paths.Currency, "total", currencyData);
                        Manager.Save(Paths.Currency, $"{DateTime.UtcNow.Day}.{DateTime.UtcNow.Month}.{DateTime.UtcNow.Year}", currencyData);
                    }
                }
                catch (Exception ex)
                {
                    Write($"Currency dispose failed: {ex.Message}", "core", LogLevel.Warning);
                }

                Write("Waiting for pending operations to complete...", "core");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Write($"Critical error during shutdown sequence: {ex}", "core", LogLevel.Error);
            }
            finally
            {
                Write("Restart sequence completed - terminating process", "core");
                Environment.Exit(force ? 5001 : 0);
            }
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            try
            {
                Write("Critical error.", "core", LogLevel.Error);
                Write(ex);
                Shutdown().RunSynchronously();
            }
            catch { }
            finally { Environment.Exit(ex.HResult); }
        }
        #endregion Other
    }
}