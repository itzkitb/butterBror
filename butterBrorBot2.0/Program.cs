using butterBror.BotUtils;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using static butterBror.Utils.Device;
using static butterBror.Utils.TwitchToken;
using DankDB;
using Microsoft.TeamFoundation.Common;

namespace butterBror
{
    public class Engine
    {
        public  static int      restarted_times    = 0;
        public  static int      completed_commands = 0;
        public  static int      users              = 0;
        public  static bool     restarting         = false;
        public  static bool     ready              = false;
        public  static float    coins              = 0;
        public  static string   version            = "2.15";
        public  static string   patch              = "A";
        public  static string   previous_version;
        public  static int      coin_dollars       = 0;
        public  static DateTime start_time         = new();
        private static float    last_coin_amount   = 0;
        public  static int      ticks              = 20;
        public  static long     ticks_counter      = 0;
        public  static double   tick_delay         = 0;
        private static bool     is_tick_ended      = true;
        public  static long     skipped_ticks      = 0;
        private static long     last_tick_count    = 0;
        public  static long     ticks_per_second   = 0;
        private static long     last_send_tick     = 0;

        private class DankDB_previous_statistics
        {
            public static long file_reads   = 0;
            public static long cache_reads  = 0;
            public static long cache_writes = 0;
            public static long file_writes  = 0;
            public static long checks       = 0;
        }

        private static Timer ticks_timer;
        private static Timer second_timer;
        private static Task  bot_task;

        public class Statistics
        {
            public static StatisticItem functions_used  = new();
            public static StatisticItem messages_readed = new();
            public class DataBase
            {
                public static StatisticItem operations   = new();
                public static StatisticItem file_reads   = new();
                public static StatisticItem cache_reads  = new();
                public static StatisticItem cache_writes = new();
                public static StatisticItem file_writes  = new();
                public static StatisticItem checks       = new();
            }
        }

        public class StatisticItem
        {
            private int per_second = 0;
            private int total      = 0;

            private DateTime last_update = DateTime.UtcNow;

            public int Get()
            {
                if ((DateTime.UtcNow - last_update).TotalSeconds >= 1)
                {
                    last_update = DateTime.UtcNow;
                    per_second = total;
                    total = 0;
                }

                return per_second;
            }

            public void Add(int count = 1)
            {
                total += count;

                if ((DateTime.UtcNow - last_update).TotalSeconds >= 1)
                {
                    last_update = DateTime.UtcNow;
                    per_second = total;
                    total = 0;
                }
            }
        }

        public static async void Start(string? mainPath = null, int customTickSpeed = 20)
        {
            Engine.Statistics.functions_used.Add();

            ticks = customTickSpeed;
            if (customTickSpeed > 1000)
            {
                Utils.Console.WriteError(new Exception("Ticks cannot exceed 1000 per second!"), "Bot/Start");
                return;
            }
            else if (customTickSpeed < 1)
            {
                Utils.Console.WriteError(new Exception("Ticks cannot be less than 1 per second!"), "Bot/Start");
                return;
            }
            int ticks_time = 1000 / ticks;

            Utils.Console.WriteLine("Please wait...", "kernel");

            string proccessor = "Unnamed processor";
            double memory = 0;
            long total_disk_memory = 0;
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    foreach (ManagementObject cpu in searcher.Get())
                    {
                        proccessor = cpu["Name"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteLine("Unable to get operating system name", "kernel");
            }

            try
            {
                memory = Utils.Memory.BytesToGB(Device.Memory.GetTotalMemoryBytes());
            }
            catch (Exception ex)
            {
                Utils.Console.WriteLine("Unable to get RAM size", "kernel");
            }

            try
            {
                foreach (DriveInfo drive in Drives.Get())
                {
                    if (drive.IsReady)
                    {
                        total_disk_memory += drive.AvailableFreeSpace;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteLine("Unable to get disks sizes", "kernel");
            }

            Utils.Console.WriteLine($@"ButterBror

            :::::::::                  
        :::::::::::::::::              
      ::i:::::::::::::::::::           
  :::::::::::::::::::::::::::::        
 ::::::::::::::::::::::::::::::::      
 {{~:::::::::::::::::::::::::::::::     v.{version}#{patch}
 0000XI::::::::::::::::::::::tC00:     {customTickSpeed} TPS
 ::c0000nI::::::::::::::::(v1::<l      {Environment.OSVersion.Platform}
 ((((:n0000f-::::::::}}x00(::n000(:     {proccessor}
 n0((::::c0000f(:::>}}X(l!00QQ0((::     RAM: {memory} GB
  :():::::::C000000000000:::::+l:      Total disks space: {Math.Round(Utils.Memory.BytesToGB(total_disk_memory))} GB
     Ix:(((((((:-}}-:((:::100_:         
        :X00:((:::::]000x;:            
            :x0000000n:                
              :::::::
", "kernel", FG: ConsoleColor.Yellow);

            Utils.Console.WriteLine($"The engine is currently starting...", "kernel");

            if (mainPath != null)
            {
                Maintenance.path_general = mainPath;
                Maintenance.path_main = mainPath + "butterBror/";

                Utils.Console.WriteLine($"Main path: {Maintenance.path_main}", "kernel");
                Utils.Console.WriteLine($"The paths are set!", "kernel");
            }
            else
            {
                Maintenance.path_general = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/ItzKITb/";
                Maintenance.path_main = Maintenance.path_general + "butterBror/";
            }

            ticks_timer  = new(OnTick, null, 0, ticks_time);
            second_timer = new((object? timer) =>
            {
                ticks_per_second = ticks_counter - last_tick_count;
                last_tick_count  = ticks_counter;
            }, null, 0, 1000);
            Utils.Console.WriteLine($"TPS counter successfully started", "kernel");

            try
            {
                start_time = DateTime.Now;
                bot_task = Task.Run(() => { Maintenance.Start(0); });

                while (true)
                {
                    if (restarting)
                    {
                        restarted_times++;
                        restarting = false;
                        Utils.Console.WriteLine("Restarting...", "kernel");
                        Restart();
                    }
                    await Task.Delay(ticks_time);
                }
            }
            catch (Exception e)
            {
                Utils.Console.WriteError(e, "BotEngine/Main");
            }
        }
        public static async void OnTick(object? timer)
        {
            Statistics.functions_used.Add();

            if (!is_tick_ended)
            {
                skipped_ticks++;
                return;
            }

            try
            {
                int cache_reads  = (int)((long)DankDB.Statistics.cache_reads  - DankDB_previous_statistics.cache_reads);
                int cache_writes = (int)((long)DankDB.Statistics.cache_writes - DankDB_previous_statistics.cache_writes);
                int writes       = (int)((long)DankDB.Statistics.writes       - DankDB_previous_statistics.file_writes);
                int reads        = (int)((long)DankDB.Statistics.reads        - DankDB_previous_statistics.file_reads);
                int checks       = (int)((long)DankDB.Statistics.checks       - DankDB_previous_statistics.checks);

                Statistics.DataBase.cache_reads.Add(cache_reads);
                Statistics.DataBase.cache_writes.Add(cache_writes);
                Statistics.DataBase.file_writes.Add(writes);
                Statistics.DataBase.file_reads.Add(reads);
                Statistics.DataBase.checks.Add(checks);
                Statistics.DataBase.operations.Add(cache_reads + cache_writes + writes + reads + checks);

                DankDB_previous_statistics.cache_reads  = (long)DankDB.Statistics.cache_reads;
                DankDB_previous_statistics.cache_writes = (long)DankDB.Statistics.cache_writes;
                DankDB_previous_statistics.file_reads   = (long)DankDB.Statistics.reads;
                DankDB_previous_statistics.file_writes  = (long)DankDB.Statistics.writes;
                DankDB_previous_statistics.checks       = (long)DankDB.Statistics.checks;

                DateTime startTime = DateTime.Now;

                if (!restarting && coins != 0 && users != 0 && last_coin_amount != coins)
                {
                    var date = DateTime.UtcNow;
                    Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", coins },
                                    { "users", users },
                                    { "dollars", coin_dollars },
                                    { "cost", coin_dollars / coins },
                                    { "middleBalance", coins / users }
                            };

                    if (!Maintenance.path_currency.IsNullOrEmpty())
                    {
                        Manager.Save(Maintenance.path_currency, "totalAmount", coins);
                        Manager.Save(Maintenance.path_currency, "totalUsers", users);
                        Manager.Save(Maintenance.path_currency, "totalDollarsInTheBank", coin_dollars);
                        Manager.Save(Maintenance.path_currency, $"[{date.Day}.{date.Month}.{date.Year}]", "");
                        Manager.Save(Maintenance.path_currency, $"[{date.Day}.{date.Month}.{date.Year}] cost", (coin_dollars / coins));
                        Manager.Save(Maintenance.path_currency, $"[{date.Day}.{date.Month}.{date.Year}] amount", coins);
                        Manager.Save(Maintenance.path_currency, $"[{date.Day}.{date.Month}.{date.Year}] users", users);
                        Manager.Save(Maintenance.path_currency, $"[{date.Day}.{date.Month}.{date.Year}] dollars", coin_dollars);
                    }

                    last_coin_amount = coins;
                }

                if (DateTime.UtcNow.Minute % 10 == 0 && DateTime.UtcNow.Second == 0 && ticks_counter - last_send_tick > ticks)
                {
                    last_send_tick = ticks_counter;
                    Maintenance.UpdatePaths();
                    Maintenance.SaveEmoteCache();
                    await Maintenance.StatusSender();

                    // Clearing semaphore
                    var now = DateTime.UtcNow;
                    var timeout = TimeSpan.FromMinutes(10);

                    foreach (var (userId, (semaphore, lastUsed)) in Command.messages_semaphores.ToList())
                    {
                        if (now - lastUsed > timeout)
                        {
                            if (Command.messages_semaphores.TryRemove(userId, out var entry))
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

                is_tick_ended = false;
                ticks_counter++;
                tick_delay = (DateTime.Now - startTime).TotalMilliseconds;
            }
            catch (Exception e)
            {
                Utils.Console.WriteError(e, "BotEngine/Tick");
            }
            finally
            {
                is_tick_ended = true;
            }
        }
        private static void Restart()
        {
            Engine.Statistics.functions_used.Add();

            Maintenance.TurnOff();
            ticks_timer?.Dispose();
            second_timer?.Dispose();
            bot_task.Dispose();

            // Переинициализация таймеров
            int ticks_time = 1000 / ticks;
            ticks_timer = new Timer(OnTick, null, 0, ticks_time);
            second_timer = new((object? timer) =>
            {
                ticks_per_second = ticks_counter - last_tick_count;
                last_tick_count = ticks_counter;
            }, null, 0, 1000);

            bot_task = Task.Run(() => { Maintenance.Start(restarted_times); });
        }
    }
    // #BOT
    public class Maintenance
    {
        public  static string? path_general { get; set; }
        private static string? _main_path;

        public static string path_main
        {
            get => _main_path;
            set
            {
                _main_path = value;
                UpdatePaths();
            }
        }
        public static string? path_channels               { get; private set; }
        public static string? path_users                  { get; private set; }
        public static string? path_bankdata               { get; private set; }
        public static string? path_nicknames_data         { get; private set; }
        public static string? path_n2id                   { get; private set; }
        public static string? path_id2n                   { get; private set; }
        public static string? path_settings               { get; private set; }
        public static string? path_cookies                { get; private set; }
        public static string? path_translations           { get; private set; }
        public static string? path_translate_default      { get; private set; }
        public static string? path_translate_custom       { get; private set; }
        public static string? path_blacklist_words        { get; private set; }
        public static string? path_blacklist_replacements { get; private set; }
        public static string? path_API_uses               { get; private set; }
        public static string? path_logs                   { get; private set; }
        public static string? path_errors                 { get; private set; }
        public static string? path_cache                  { get; private set; }
        public static string? path_currency               { get; private set; }
        public static string? path_7tv_cache              { get; private set; }
        public static string? path_reserve_copies         { get; private set; }

        private static string?    token_telegram;
        public  static TokenData? token_twitch;
        private static string?    token_discord;
        public  static string?    twitch_secret_token;
        public  static string?    token_imgur;
        public  static string?    token_7tv;

        public static string telegram_url = "telegram.org";
        public static string twitch_url   = "twitch.tv";
        public static string discord_url  = "discord.com";

        public static string?  bot_name;
        public static string?  twitch_client_id;
        public static string?  current_color;
        public static string[] channels_connect_announcement        = [];
        public static string[] channels_reconnect_announcement      = [];
        public static string[] channels_version_change_announcement = [];
        public static string[] channels_list       = [];
        public static int      connected_servers   = 0;
        public static int      proccessed_messages = 0;
        public static bool     connected           = false;
        public static bool     twitch_reconnected  = false;
        public static string   coin_symbol         = "🥪";
        public static int      currency_mentioned  = 8;
        public static int      currency_mentioner  = 2;
        public static char     executor            = '#';
         
        public static Dictionary<string, string> users_7tv_ids = new Dictionary<string, string>();

        public  static TwitchToken?            twitch_token_getter;
        public  static TwitchClient            twitch_client               = new();
        public  static DiscordSocketClient?    discord_client;
        public  static CommandService?         discord_command_service;
        public  static ReceiverOptions?        telegram_receiver_options;
        public  static IServiceProvider?       discord_service_provider;
        public  static ITelegramBotClient?     telegram_client;
        private static DateTime                bot_start_time              = DateTime.UtcNow;
        private static CancellationTokenSource telegram_cancellation_token = new CancellationTokenSource();
        private static PerformanceCounter      cpu_counter                 = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        

        public static SevenTV.SevenTV                                                                sevenTv             = new SevenTV.SevenTV();
        public static SevenTvService                                                                 sevenTvService      = new SevenTvService(new HttpClient());
        public static ConcurrentDictionary<string, (List<string> emotes, DateTime expiration)>       channels_7tv_emotes = new();
        public static ConcurrentDictionary<string, (string setId, DateTime expiration)>              emoteSetCache       = new();
        public static ConcurrentDictionary<string, (string userId, DateTime expiration)>             userSearchCache     = new();
        public static ConcurrentDictionary<string, (SevenTV.Types.Emote emote, DateTime expiration)> emoteCache          = new();
        public static readonly TimeSpan                                                              CacheTTL            = TimeSpan.FromMinutes(30);

        public static async void Start(int ThreadID)
        {
            Engine.Statistics.functions_used.Add();
            cpu_counter.NextValue();
            bot_start_time            = DateTime.UtcNow;
            Thread.CurrentThread.Name = ThreadID.ToString();
            Engine.ready              = false;

            // START
            if (FileUtil.FileExists(path_currency))
            {
                Engine.coins = Manager.Get<float>(path_currency, "totalAmount");
                Engine.coin_dollars = Manager.Get<int>(path_currency, "totalDollarsInTheBank");
                Engine.users = Manager.Get<int>(path_currency, "totalUsers");
            }

            try
            {
                Utils.Console.WriteLine("Checking directories right now...", "main");
                string[] directories = {
                        path_general, path_main, path_channels, path_users, path_nicknames_data,
                        path_n2id, path_id2n, path_translate_default, path_translate_custom, path_bankdata
                    };
                foreach (var dir in directories)
                {
                    FileUtil.CreateDirectory(dir);
                }
                string[] directories_with_platforms = {
                        path_channels, path_users, path_nicknames_data, path_translate_default, path_translate_custom, path_bankdata
                    };


                Utils.Console.WriteLine("Checking files right now...", "main");
                if (!FileUtil.FileExists(path_settings))
                {
                    InitializeSettingsFile(path_settings);
                    Utils.Console.WriteLine($"The settings file has been created! ({path_settings})", "main", ConsoleColor.Black, ConsoleColor.Cyan);
                    Thread.Sleep(-1);
                }
                else
                {
                    string[] files = {
                            path_cookies, path_blacklist_words, path_blacklist_replacements,
                            path_currency, path_cache, path_logs, path_API_uses,
                            Path.Combine(path_translate_default, "ru.json"),
                            Path.Combine(path_translate_default, "en.json"), Path.Combine(path_main, "VERSION.txt")
                    };

                    foreach (var file in files)
                    {
                        FileUtil.CreateFile(file);
                    }
                    /* Useless for now
                    if (!System.IO.File.Exists(path_7tv_cache))
                    {
                        Initialize7TVCache(path_7tv_cache);
                    }
                    */

                    Engine.previous_version = File.ReadAllText(Path.Combine(path_main, "VERSION.txt"));
                    File.WriteAllText(Path.Combine(path_main, "VERSION.txt"), $"{Engine.version}#{Engine.patch}");

                    Commands.IndexCommands();
                    await LoadSettings();
                    Utils.Console.WriteLine("Settings loaded!", "main");
                    Utils.Console.WriteLine(twitch_client_id, "main");
                    twitch_token_getter = new(twitch_client_id, twitch_secret_token, path_main + "TWITCH_AUTH.json");
                    var token = await TwitchToken.GetTokenAsync();
                    if (token != null)
                    {
                        token_twitch = token;
                        Connect();
                    }
                    else
                    {
                        Restart();
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "bot_maintrance");
                Restart();
            }
        }
        public static async Task Connect()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Utils.Console.WriteLine("Connecting to Twitch...", "main");
                await ConnectToTwitch();

                Utils.Console.WriteLine("Connecting to Discord...", "main");
                await ConnectToDiscord();

                Utils.Console.WriteLine("Connecting to Telegram...", "main");
                await ConnectToTelegram();

                Utils.Console.WriteLine("Loading 7tv cache...", "main");
                LoadEmoteCache();

                DateTime endTime = DateTime.UtcNow;
                Engine.ready = true;
                Utils.Console.WriteLine($"Well done! (Started in {(endTime - bot_start_time).TotalMilliseconds} ms)", "main");
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "bot_connect");
                Restart();
            }
        }
        private static async Task ConnectToTwitch()
        {
            Engine.Statistics.functions_used.Add();
            var credentials = new ConnectionCredentials(bot_name, "oauth:" + token_twitch.AccessToken);
            var client_options = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            var webSocket_client = new WebSocketClient(client_options);
            twitch_client = new TwitchClient(webSocket_client);
            twitch_client.Initialize(credentials, bot_name, executor);

            twitch_client.OnJoinedChannel += twitch_events.OnJoin;
            twitch_client.OnChatCommandReceived += Commands.Twitch;
            twitch_client.OnMessageReceived += twitch_events.OnMessageReceived;
            twitch_client.OnMessageThrottled += twitch_events.OnMessageThrottled;
            twitch_client.OnMessageSent += twitch_events.OnMessageSend;
            twitch_client.OnAnnouncement += twitch_events.OnAnnounce;
            twitch_client.OnBanned += twitch_events.OnBanned;
            twitch_client.OnConnectionError += twitch_events.OnConnectionError;
            twitch_client.OnContinuedGiftedSubscription += twitch_events.OnContinuedGiftedSubscription;
            twitch_client.OnChatCleared += twitch_events.OnChatCleared;
            twitch_client.OnDisconnected += twitch_events.OnTwitchDisconnected;
            twitch_client.OnReconnected += twitch_events.OnReconnected;
            twitch_client.OnError += twitch_events.OnError;
            twitch_client.OnIncorrectLogin += twitch_events.OnIncorrectLogin;
            twitch_client.OnLeftChannel += twitch_events.OnLeftChannel;
            twitch_client.OnRaidNotification += twitch_events.OnRaidNotification;
            twitch_client.OnNewSubscriber += twitch_events.OnNewSubscriber;
            twitch_client.OnGiftedSubscription += twitch_events.OnGiftedSubscription;
            twitch_client.OnCommunitySubscription += twitch_events.OnCommunitySubscription;
            twitch_client.OnReSubscriber += twitch_events.OnReSubscriber;
            twitch_client.OnSuspended += twitch_events.OnSuspended;
            twitch_client.OnConnected += twitch_events.OnConnected;
            twitch_client.OnLog += twitch_events.OnLog;

            twitch_client.Connect();

            var not_founded_channels = new List<string>();
            string send_channels = string.Join(", ", channels_list.Select(channel =>
            {
                var channel2 = Names.GetUsername(channel, Platforms.Twitch);
                if (channel2 == null) not_founded_channels.Add(channel);
                return channel2;
            }).Where(channel => channel != "NONE\n"));

            Utils.Console.WriteLine($"[TW] Connecting to {send_channels}", "main");
            foreach (var channel in channels_list)
            {
                var channel2 = Names.GetUsername(channel, Platforms.Twitch);
                if (channel2 != null) twitch_client.JoinChannel(channel2);
            }
            foreach (var channel in not_founded_channels) Utils.Console.WriteLine("[TW] Can't find ID for " + channel, "err", ConsoleColor.Red);

            twitch_client.JoinChannel(bot_name.ToLower());
            twitch_client.SendMessage(bot_name.ToLower(), "truckCrash Connecting to twitch...");
        }
        private static async Task ConnectToDiscord()
        {
            Engine.Statistics.functions_used.Add();
            var discordConfig = new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent
            };
            discord_client = new DiscordSocketClient(discordConfig);
            discord_command_service = new CommandService();
            discord_service_provider = new ServiceCollection()
                .AddSingleton(discord_client)
                .AddSingleton(discord_command_service)
                .BuildServiceProvider();

            discord_client.Log += DiscordEventHandler.LogAsync;
            discord_client.JoinedGuild += DiscordEventHandler.ConnectToGuilt;
            discord_client.Ready += DiscordWorker.ReadyAsync;
            discord_client.MessageReceived += DiscordWorker.MessageReceivedAsync;
            discord_client.SlashCommandExecuted += DiscordEventHandler.SlashCommandHandler;
            discord_client.ApplicationCommandCreated += DiscordEventHandler.ApplicationCommandCreated;
            discord_client.ApplicationCommandDeleted += DiscordEventHandler.ApplicationCommandDeleted;
            discord_client.ApplicationCommandUpdated += DiscordEventHandler.ApplicationCommandUpdated;
            discord_client.ChannelCreated += DiscordEventHandler.ChannelCreated;
            discord_client.ChannelDestroyed += DiscordEventHandler.ChannelDeleted;
            discord_client.ChannelUpdated += DiscordEventHandler.ChannelUpdated;
            discord_client.Connected += DiscordEventHandler.Connected;
            discord_client.ButtonExecuted += DiscordEventHandler.ButtonTouched;

            await DiscordWorker.RegisterCommandsAsync();
            await discord_client.LoginAsync(TokenType.Bot, token_discord);
            await discord_client.StartAsync();
        }
        private static async Task ConnectToTelegram()
        {
            Engine.Statistics.functions_used.Add();
            telegram_client = new TelegramBotClient(token_telegram);
            telegram_receiver_options = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                DropPendingUpdates = true,
            };

            telegram_client.StartReceiving(TelegramWorker.UpdateHandler, TelegramWorker.ErrorHandler, telegram_receiver_options, telegram_cancellation_token.Token);
        }
        public static async Task StatusSender()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                DateTime start = DateTime.UtcNow;
                Utils.Console.WriteLine("[TW] Status sender started!", "kernel", ConsoleColor.Red);

                System.Net.NetworkInformation.Ping ping = new();
                PingReply twitch = ping.Send("twitch.tv", 1000);
                PingReply discord = ping.Send("discord.com", 1000);
                PingReply telegram = ping.Send("t.me", 1000);
                PingReply ISP = ping.Send("192.168.1.1", 1000);

                if (ISP.Status != IPStatus.Success)
                {
                    ISP = ping.Send("192.168.0.1", 1000);
                    if (ISP.Status != IPStatus.Success) Utils.Console.WriteLine("[TW] Error ISP ping: " + ISP.Status.ToString(), "err");
                }

                long memory = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

                Utils.Chat.TwitchSend(bot_name.ToLower(), $"/me glorp 📡 Twitch: {twitch.RoundtripTime}ms | " +
                    $"Discord: {discord.RoundtripTime}ms | " +
                    $"Telegram: {telegram.RoundtripTime}ms | " +
                    $"ISP: {ISP.RoundtripTime}ms | " +
                    $"⌚ {DateTime.Now - Engine.start_time:dd\\:hh\\:mm\\.ss} | " +
                    $"📦 {memory}mb | " +
                    $"🔋 {Battery.GetBatteryCharge()}% ({Battery.IsCharging()}) | " +
                    $"🔥 {cpu_counter.NextValue():0.00}% | " +
                    $"TPS: {Engine.ticks_per_second} | " +
                    $"TT: {Engine.ticks_counter} | " +
                    $"ST: {Engine.skipped_ticks}", "", "", "", true);

                try
                {
                    var newToken = await RefreshAccessToken(token_twitch);
                    if (newToken != null) token_twitch = newToken;
                }
                catch (Exception ex)
                {
                    Utils.Console.WriteError(ex, "StatusSender/TokenRefresher");
                }

                Utils.Console.WriteLine($"[TW] Status sender ended! (In {(DateTime.UtcNow - start).TotalMilliseconds}ms)", "kernel", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "Bot/StatusSender");
            }
        }
        public static int Restart()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Utils.Console.WriteLine("Restarting...", "main");
                Disconnect();
                return 1;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteLine($"Restart error: {ex.Message} : {ex.StackTrace}", "kernel");
                return 0;
            }
        }
        public static int TurnOff()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                twitch_client.SendMessage(bot_name, "Zevlo Turning off...");

                foreach (var channel in channels_connect_announcement)
                {
                    try
                    {
                        twitch_client.SendMessage(Names.GetUsername(channel, Platforms.Twitch), "Zevlo Turning off...");
                    }
                    catch { }
                }

                Disconnect();
                Utils.Console.WriteLine($"Bot is disabled!", "kernel", ConsoleColor.Black, ConsoleColor.Cyan);
                return 1;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteLine($"Turn off error: {ex.Message} : {ex.StackTrace}", "kernel");
                return 0;
            }
        }
        private static void Disconnect()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                foreach (var channel in twitch_client.JoinedChannels)
                {
                    try
                    {
                        twitch_client.LeaveChannel(channel);
                    }
                    catch (Exception ex)
                    {
                        Utils.Console.WriteLine($"[TW] Leave channel error: {ex.Message} : {ex.StackTrace}", "main");
                    }
                }

                twitch_client.Disconnect();
                telegram_cancellation_token.Dispose();
                discord_client.Dispose();
            }
            catch (Exception ex)
            {
                Utils.Console.WriteLine($"[TW] Disconnect error: {ex.Message} : {ex.StackTrace}", "main");
            }

            twitch_client = null;
            telegram_client = null;
            discord_client = null;
            Engine.ready = false;
            Engine.restarting = true;
        }
        public static void UpdatePaths()
        {
            Engine.Statistics.functions_used.Add();
            path_channels = Path.Combine(path_main, "CHNLS/");
            path_users = Path.Combine(path_main, "USERSDB/");
            path_bankdata = Path.Combine(path_main, "BANKDB/");
            path_nicknames_data = Path.Combine(path_main, "CONVRT/");
            path_n2id = Path.Combine(path_nicknames_data, "N2I/");
            path_id2n = Path.Combine(path_nicknames_data, "I2N/");
            path_settings = Path.Combine(path_main, "SETTINGS.json");
            path_cookies = Path.Combine(path_main, "COOKIES.MDS");
            path_translations = Path.Combine(path_main, "TRNSLT/");
            path_translate_default = Path.Combine(path_translations, "DEFAULT/");
            path_translate_custom = Path.Combine(path_translations, "CUSTOM/");
            path_blacklist_words = Path.Combine(path_main, "BNWORDS.txt");
            path_blacklist_replacements = Path.Combine(path_main, "BNWORDSREP.txt");
            path_API_uses = Path.Combine(path_main, "API.json");
            path_logs = Path.Combine(path_main, "LOGS.log");
            path_errors = Path.Combine(path_main, "ERRORS.log");
            path_cache = Path.Combine(path_main, "LOC.cache");
            path_currency = Path.Combine(path_main, "CURR.json");
            path_7tv_cache = Path.Combine(path_main, "7TV.json");
            path_reserve_copies = Path.Combine(path_general, "butterbror_reserves/", $"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month}{DateTime.UtcNow.Day}/", $"{DateTime.UtcNow.Hour}/");
        }
        private static void InitializeSettingsFile(string path)
        {
            Engine.Statistics.functions_used.Add();
            FileUtil.CreateFile(path);
            Manager.Save(path, "bot_name", "");
            Manager.Save(path, "discord_token", "");
            Manager.Save(path, "imgur_token", "");
            Manager.Save(path, "user_id", "");
            Manager.Save(path, "client_id", "");
            Manager.Save(path, "twitch_secret_token", "");
            Manager.Save(path, "twitch_connect_message_channels", Array.Empty<string>());
            Manager.Save(path, "twitch_reconnect_message_channels", Array.Empty<string>());
            Manager.Save(path, "twitch_connect_channels", new[] { "First channel", "Second channel" });
            string[] apis = { "First api", "Second api" };
            Manager.Save(path, "weather_token", apis);
            Manager.Save(path, "gpt_tokens", apis);
            Manager.Save(path, "telegram_token", "");
            Manager.Save(path, "twitch_version_message_channels", Array.Empty<string>());
            Manager.Save(path, "7tv_token", "");
            Manager.Save(path, "coin_symbol", "\U0001f96a");
            Manager.Save(path, "currency_mentioned_payment", 8);
            Manager.Save(path, "currency_mentioner_payment", 2);
        }
        private static async Task LoadSettings()
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("Loading files...", "main");
            bot_name = Manager.Get<string>(path_settings, "bot_name");
            channels_list = Manager.Get<string[]>(path_settings, "twitch_connect_channels");
            channels_reconnect_announcement = Manager.Get<string[]>(path_settings, "twitch_reconnect_message_channels");
            channels_connect_announcement = Manager.Get<string[]>(path_settings, "twitch_connect_message_channels");
            token_discord = Manager.Get<string>(path_settings, "discord_token");
            token_imgur = Manager.Get<string>(path_settings, "imgur_token");
            twitch_client_id = Manager.Get<string>(path_settings, "client_id");
            twitch_secret_token = Manager.Get<string>(path_settings, "twitch_secret_token");
            token_telegram = Manager.Get<string>(path_settings, "telegram_token");
            channels_version_change_announcement = Manager.Get<string[]>(path_settings, "twitch_version_message_channels");
            token_7tv = Manager.Get<string>(path_settings, "7tv_token");
            users_7tv_ids = Manager.Get<Dictionary<string, string>>(path_7tv_cache, "Ids");
            coin_symbol = Manager.Get<string>(path_settings, "coin_symbol");
            currency_mentioned = Manager.Get<int>(path_settings, "currency_mentioned_payment");
            currency_mentioner = Manager.Get<int>(path_settings, "currency_mentioner_payment");
            executor = Convert.ToChar(Manager.Get<string>(path_settings, "executor"));
        }
        public static void SaveEmoteCache()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                var data = new
                {
                    Channels7tvEmotes = channels_7tv_emotes.ToDictionary(kv => kv.Key, kv => kv.Value),
                    EmoteSetCache = emoteSetCache.ToDictionary(kv => kv.Key, kv => kv.Value),
                    UserSearchCache = userSearchCache.ToDictionary(kv => kv.Key, kv => kv.Value),
                    EmoteCache = emoteCache.ToDictionary(kv => kv.Key, kv => kv.Value)
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(Maintenance.path_7tv_cache));
                FileUtil.SaveFileContent(Maintenance.path_7tv_cache, json);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "Mainterance/SaveEmoteCache");
            }
        }
        public static void LoadEmoteCache()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                if (!FileUtil.FileExists(Maintenance.path_7tv_cache)) return;

                string json = FileUtil.GetFileContent(Maintenance.path_7tv_cache);
                var template = new
                {
                    Channels7tvEmotes = new Dictionary<string, (List<string> emotes, DateTime expiration)>(),
                    EmoteSetCache = new Dictionary<string, (string setId, DateTime expiration)>(),
                    UserSearchCache = new Dictionary<string, (string userId, DateTime expiration)>(),
                    EmoteCache = new Dictionary<string, (SevenTV.Types.Emote emote, DateTime expiration)>()
                };

                var data = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(json, template);

                channels_7tv_emotes = new ConcurrentDictionary<string, (List<string>, DateTime)>(data.Channels7tvEmotes);
                emoteSetCache = new ConcurrentDictionary<string, (string, DateTime)>(data.EmoteSetCache);
                userSearchCache = new ConcurrentDictionary<string, (string, DateTime)>(data.UserSearchCache);
                emoteCache = new ConcurrentDictionary<string, (SevenTV.Types.Emote, DateTime)>(data.EmoteCache);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "Mainterance/LoadEmoteCache");
            }
        }
    }
    public class DiscordWorker
    {
        public static async Task ReadyAsync()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Utils.Console.WriteLine($"[DS] Connected as {Maintenance.discord_client.CurrentUser}!", "discord");
                foreach (var guild in Maintenance.discord_client.Guilds)
                {
                    Utils.Console.WriteLine($"[DS] Connected to server: {guild.Name}", "discord");
                    Maintenance.connected_servers++;
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "DiscordWorker\\ReadyAsync");
            }
        }
        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                if (!(message is SocketUserMessage msg) || message.Author.IsBot) return;
                OnMessageReceivedArgs e = default;
                await Command.ProcessMessageAsync(message.Author.Id.ToString(), ((SocketGuildChannel)message.Channel).Guild.Id.ToString(), message.Author.Username.ToLower(), message.Content, e, ((SocketGuildChannel)message.Channel).Guild.Name, Platforms.Discord, null, message.Channel.ToString());
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"DiscordWorker\\MessageReceivedAsync#{message.Content}");
            }
        }
        public static async Task RegisterCommandsAsync()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Maintenance.discord_client.Ready += RegisterSlashCommands;
                Maintenance.discord_client.MessageReceived += DiscordEventHandler.HandleCommandAsync;
                await Maintenance.discord_command_service.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: Maintenance.discord_service_provider);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "DiscordWorker\\RegisterCommandsAsync");
            }
        }
        private static async Task RegisterSlashCommands()
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Updating commands...", "discord");
            await Maintenance.discord_client.Rest.DeleteAllGlobalCommandsAsync();

            await Maintenance.discord_client.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Check bot status")
                .Build());
            await Maintenance.discord_client.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("status")
                .WithDescription("View the bot's status. (Bot administrators only)")
                .Build());
            await Maintenance.discord_client.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("weather")
                .WithDescription("Check the weather")
                .AddOption("location", ApplicationCommandOptionType.String, "weather check location", isRequired: false)
                .AddOption("showpage", ApplicationCommandOptionType.Integer, "show weather on page", isRequired: false)
                .AddOption("page", ApplicationCommandOptionType.Integer, "show the result page of the received weather", isRequired: false)
                .Build());
            Utils.Console.WriteLine("[DS] All commands are registered!", "discord");
        }
    }
    public class DiscordEventHandler
    {
        public static Task LogAsync(LogMessage log)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Utils.Console.WriteLine(log.ToString().Replace("\n", " ").Replace("\r", ""), "discord");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"DiscordEventHandler\\LogAsync#{log.Message}");
                return Task.CompletedTask;
            }
        }
        public static async Task ConnectToGuilt(SocketGuild g)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[DS] Connected to a server: {g.Name}", "discord");
            Maintenance.connected_servers++;
        }
        public static async Task HandleCommandAsync(SocketMessage arg)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                var message = arg as SocketUserMessage;
                if (message == null || message.Author.IsBot) return;

                int argPos = 0;
                if (message.HasCharPrefix(Maintenance.executor, ref argPos))
                {
                    var context = new SocketCommandContext(Maintenance.discord_client, message);
                    var result = await Maintenance.discord_command_service.ExecuteAsync(context, argPos, Maintenance.discord_service_provider);
                    if (!result.IsSuccess)
                    {
                        Utils.Console.WriteLine(result.ErrorReason, "discord", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"DiscordEventHandler\\HandleCommandAsync");
            }
        }
        public static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Engine.Statistics.functions_used.Add();
            Commands.Discord(command);
        }
        public static async Task ApplicationCommandCreated(SocketApplicationCommand e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] The command has been created: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ApplicationCommandDeleted(SocketApplicationCommand e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Command deleted: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ApplicationCommandUpdated(SocketApplicationCommand e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Command updated: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ChannelCreated(SocketChannel e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] New channel created: " + e.Id, "discord");
        }
        public static async Task ChannelDeleted(SocketChannel e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] The channel has been deleted: " + e.Id, "discord");
        }
        public static async Task ChannelUpdated(SocketChannel e, SocketChannel a)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Channel updated: " + e.Id + "/" + a.Id, "discord");
        }
        public static async Task Connected()
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Connected!", "discord");
        }
        public static async Task ButtonTouched(SocketMessageComponent e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[DS] A button was pressed. User: {e.User}, Button ID: {e.Id}, Server: {((SocketGuildChannel)e.Channel).Guild.Name}", "info");
        }
    }
    public class TelegramWorker
    {
        public static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                if (update.Type != UpdateType.Message) return;

                Message message = update.Message;
                User user = message.From;
                Telegram.Bot.Types.Chat chat = message.Chat;
                User meData = Maintenance.telegram_client.GetMe().Result;

                Utils.Console.WriteLine($"{chat.Title}/{user.Username}: {message.Text}", "tg_chat");
                await Command.ProcessMessageAsync(user.Id.ToString(), chat.Id.ToString(), user.Username.ToLower(), (message.Text == null ? "[ Not text ]" : message.Text), new OnMessageReceivedArgs(), (chat.Title == null ? meData.Username : chat.Title), Platforms.Telegram, message);

                string lang = UsersData.Get<string>(user.Id.ToString(), "language", Platforms.Telegram);
                if (lang == null) lang = "ru";

                if (message.Text == "/start" || message.Text == "/start@" + meData.Username)
                {
                    await botClient.SendMessage(
                        chat.Id,
                        TranslationManager.GetTranslation(lang, "telegram:welcome", chat.Id.ToString(), Platforms.Telegram, new() {
                        { "ID", user.Id.ToString() },
                        { "WorkTime", TextUtil.FormatTimeSpan(DateTime.Now - Engine.start_time, lang) },
                        { "Version", Engine.version },
                        { "Ping", new System.Net.NetworkInformation.Ping().Send(Maintenance.telegram_url, 1000).RoundtripTime.ToString() } }),
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text == "/ping" || message.Text == "/ping@" + meData.Username)
                {
                    var workTime = DateTime.Now - Engine.start_time;
                    PingReply reply = new System.Net.NetworkInformation.Ping().Send(Maintenance.telegram_url, 1000);
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:ping", chat.Id.ToString(), Platforms.Telegram, new(){
                        { "version", Engine.version },
                        { "workTime", TextUtil.FormatTimeSpan(workTime, lang) },
                        { "tabs", Maintenance.channels_list.Length.ToString() },
                        { "loadedCMDs", Commands.commands.Count().ToString() },
                        { "completedCMDs", Engine.completed_commands.ToString() },
                        { "ping", reply.RoundtripTime.ToString() }
                    });
                    await botClient.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text == "/help" || message.Text == "/help@" + meData.Username)
                {
                    string returnMessage = TranslationManager.GetTranslation(lang, "text:bot_info", chat.Id.ToString(), Platforms.Telegram);
                    await botClient.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text == "/commands" || message.Text == "/commands@" + meData.Username)
                {
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:help", chat.Id.ToString(), Platforms.Telegram);
                    await botClient.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text.StartsWith(Maintenance.executor))
                {
                    message.Text = message.Text[1..];
                    Commands.Telegram(message);
                }

                return;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "TelegramWorker\\UpdateHandler");
            }
        }
        public static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            Engine.Statistics.functions_used.Add();
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Utils.Console.WriteError(error, "TelegramWorker\\ErrorHandler");
            return Task.CompletedTask;
        }
    }
}