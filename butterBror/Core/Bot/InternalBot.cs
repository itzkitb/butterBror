using DankDB;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using static butterBror.Engine.Statistics;
using static butterBror.Core.Bot.Console;
using static butterBror.Core.Bot.TwitchToken;
using butterBror.Data;
using butterBror.Events;
using butterBror.Services.External;
using butterBror.Services.System;
using butterBror.Workers;
using butterBror.Core.Commands;
using butterBror.Utils;
using butterBror.Models;
using butterBror.Models.DataBase;

namespace butterBror.Core.Bot
{
    /// <summary>
    /// Core bot implementation handling initialization, platform connections, and lifecycle management.
    /// </summary>
    public class InternalBot
    {
        internal ClientWorker Clients = new ClientWorker();
        public PathWorker Pathes = new PathWorker();
        internal Tokens Tokens = new Tokens();

        public bool TwitchReconnected = false;
        public bool Initialized = false;
        public bool Connected = false;

        public string TwitchClientId = string.Empty;
        public string CoinSymbol = "🥪";
        public string BotName = string.Empty;
        public char Executor = '#';

        public string[] TwitchNewVersionAnnounce = [];
        public string[] TwitchReconnectAnnounce = [];
        public string[] TwitchConnectAnnounce = [];
        public string[] TwitchChannels = [];

        public ulong MessagesProccessed = 0;
        public ulong DiscordServers = 0;
        public ulong TelegramChats = 0;
        public int CurrencyMentioned = 8;
        public int CurrencyMentioner = 2;

        public ConcurrentDictionary<string, (SevenTV.Types.Rest.Emote emote, DateTime expiration)> EmotesCache = new();
        public ConcurrentDictionary<string, (List<string> emotes, DateTime expiration)> ChannelsSevenTVEmotes = new();
        public ConcurrentDictionary<string, (string userId, DateTime expiration)> UsersSearchCache = new();
        public ConcurrentDictionary<string, (string setId, DateTime expiration)> EmoteSetsCache = new();
        public Dictionary<string, string> UsersSevenTVIDs = new Dictionary<string, string>();

        public IServiceProvider? DiscordServiceProvider;
        public ReceiverOptions? TelegramReceiverOptions;
        public CommandService? DiscordCommandService;
        public SevenTvService SevenTvService = new SevenTvService(new HttpClient());
        private DateTime _startTime = DateTime.UtcNow;

        public readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Starts the bot instance and initializes all components.
        /// </summary>
        /// <param name="ThreadID">Unique identifier for this bot instance thread.</param>
        /// <remarks>
        /// - Loads currency statistics and creates necessary directories
        /// - Initializes settings and connects to Twitch
        /// - Handles version tracking and file persistence
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "Start")]
        public async void Start()
        {
            FunctionsUsed.Add();

            _startTime = DateTime.UtcNow;
            Engine.Ready = false;

            if (FileUtil.FileExists(Pathes.Currency))
            {
                Engine.Coins = Manager.Get<float>(Pathes.Currency, "totalAmount");
                Engine.BankDollars = Manager.Get<int>(Pathes.Currency, "totalDollarsInTheBank");
                Engine.Users = Manager.Get<int>(Pathes.Currency, "totalUsers");
            }

            try
            {
                Write("Checking directories right now...", "initialization");
                string[] directories = {
                        Pathes.General, Pathes.Main, Pathes.Channels, Pathes.Users, Pathes.NicknamesData,
                        Pathes.Nick2ID, Pathes.ID2Nick, Pathes.TranslateDefault, Pathes.TranslateCustom
                    };
                foreach (var dir in directories)
                {
                    FileUtil.CreateDirectory(dir);
                }
                string[] directories_with_platforms = {
                        Pathes.Channels, Pathes.Users, Pathes.NicknamesData, Pathes.TranslateDefault, Pathes.TranslateCustom
                    };


                Write("Checking files right now...", "initialization");
                if (!FileUtil.FileExists(Pathes.Settings))
                {
                    InitializeSettingsFile(Pathes.Settings);
                    Write($"The settings file has been created! ({Pathes.Settings})", "initialization");
                    Thread.Sleep(-1);
                }

                string[] files = {
                            Pathes.Cookies, Pathes.BlacklistWords, Pathes.BlacklistReplacements,
                            Pathes.Currency, Pathes.Cache, Pathes.Logs, Pathes.APIUses,
                            Path.Combine(Pathes.TranslateDefault, "ru.json"),
                            Path.Combine(Pathes.TranslateDefault, "en.json"), Path.Combine(Pathes.Main, "VERSION.txt")
                    };

                Write("Creating files...", "initialization");
                foreach (var file in files)
                {
                    FileUtil.CreateFile(file);
                }

                Engine.PreviousVersion = File.ReadAllText(Path.Combine(Pathes.Main, "VERSION.txt"));
                File.WriteAllText(Path.Combine(Pathes.Main, "VERSION.txt"), $"{Engine.Version}.{Engine.Patch}");

                Write("Loading settigns...", "initialization");
                LoadSettings();

                Tokens.TwitchGetter = new(TwitchClientId, Tokens.TwitchSecretToken, Pathes.Main + "TWITCH_AUTH.json");
                var token = await GetTokenAsync();

                if (token != null)
                {
                    Tokens.Twitch = token;
                    Connect();
                }
                else
                {
                    Write("Twitch token is null! Something went wrong...", "initialization");
                    Restart();
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                Restart();
            }
        }

        /// <summary>
        /// Establishes connections to all supported platforms.
        /// </summary>
        /// <remarks>
        /// - Connects to Twitch with authentication
        /// - Initializes Discord with command handlers
        /// - Starts Telegram message reception
        /// - Loads cached emote data
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "Connect")]
        public async Task Connect()
        {
            FunctionsUsed.Add();

            try
            {
                Write("Connecting to Twitch...", "initialization");
                ConnectToTwitch();

                Write("Connecting to Discord...", "initialization");
                await ConnectToDiscord();

                Write("Connecting to Telegram...", "initialization");
                ConnectToTelegram();

                Write("Loading 7tv cache...", "initialization");
                LoadEmoteCache();

                DateTime endTime = DateTime.UtcNow;
                Engine.Ready = true;
                Initialized = true;
                Connected = true;
                Write($"Well done! ({(endTime - _startTime).TotalMilliseconds} ms)", "initialization");
            }
            catch (Exception ex)
            {
                Write(ex);
                Restart();
            }
        }

        /// <summary>
        /// Establishes connection to Twitch platform with event subscriptions.
        /// </summary>
        /// <remarks>
        /// - Sets up Twitch client credentials and event handlers
        /// - Joins configured channels and the bot's own channel
        /// - Sends connection announcement message
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "ConnectToTwitch")]
        private void ConnectToTwitch()
        {
            FunctionsUsed.Add();

            var credentials = new ConnectionCredentials(BotName, "oauth:" + Tokens.Twitch.AccessToken);
            var client_options = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            var webSocket_client = new WebSocketClient(client_options);
            Clients.Twitch = new TwitchClient(webSocket_client);
            Clients.Twitch.Initialize(credentials, BotName, Executor);

            #region Events subscription
            Clients.Twitch.OnJoinedChannel += TwitchEvents.OnJoin;
            Clients.Twitch.OnChatCommandReceived += Commands.Executor.Twitch;
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
            #endregion

            Clients.Twitch.Connect();

            var not_founded_channels = new List<string>();
            string send_channels = string.Join(", ", TwitchChannels.Select(channel =>
            {
                var channel2 = Names.GetUsername(channel, PlatformsEnum.Twitch);
                if (channel2 == null) not_founded_channels.Add(channel);
                return channel2;
            }).Where(channel => channel != "NONE\n"));

            Write($"Twitch - Connecting to {send_channels}", "initialization");
            foreach (var channel in TwitchChannels)
            {
                var channel2 = Names.GetUsername(channel, PlatformsEnum.Twitch);
                if (channel2 != null) Clients.Twitch.JoinChannel(channel2);
            }
            foreach (var channel in not_founded_channels)
                Write("Twitch - Can't find ID for " + channel, "initialization", LogLevel.Warning);

            Clients.Twitch.JoinChannel(BotName.ToLower());
            Clients.Twitch.SendMessage(BotName.ToLower(), "truckCrash Connecting to twitch...");
        }

        /// <summary>
        /// Establishes connection to Discord platform with command registration.
        /// </summary>
        /// <remarks>
        /// - Initializes Discord client with required gateway intents
        /// - Registers command handlers and event subscriptions
        /// - Starts the Discord bot instance
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "ConnectToDiscord")]
        private async Task ConnectToDiscord()
        {
            FunctionsUsed.Add();

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
        }

        /// <summary>
        /// Starts receiving messages from Telegram platform.
        /// </summary>
        /// <remarks>
        /// - Initializes Telegram bot client
        /// - Starts message reception with configured options
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "ConnectToTelegram")]
        private void ConnectToTelegram()
        {
            FunctionsUsed.Add();

            Clients.Telegram = new TelegramBotClient(Tokens.Telegram);
            TelegramReceiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                DropPendingUpdates = true,
            };

            Clients.Telegram.StartReceiving(Events.TelegramEvents.UpdateHandler, Events.TelegramEvents.ErrorHandler, TelegramReceiverOptions, Clients.TelegramCancellationToken.Token);
        }

        /// <summary>
        /// Sends periodic telemetry data to Twitch chat and logs system metrics.
        /// </summary>
        /// <remarks>
        /// - Collects performance metrics (memory, CPU, network pings)
        /// - Reports command execution times and database operations
        /// - Sends detailed metrics to bot's Twitch channel
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "StatusSender")]
        public async Task SendTelemetry()
        {
            FunctionsUsed.Add();

            try
            {
                Write("Twitch - Telemetry started!", "telemetry");
                Chat.TwitchSend(BotName.ToLower(), $"glorp 📡 ᴛᴇʟᴇᴍᴇᴛʀʏ sᴛᴀʀᴛᴇᴅ...", "", "", "", true, false);
                Stopwatch Start = Stopwatch.StartNew();

                int cacheItemsBefore = Worker.cache.count;
                long memoryBefore = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
                Worker.cache.Clear(TimeSpan.FromMinutes(10));

                #region Ethernet ping
                Ping ping = new();
                PingReply twitch = ping.Send(URLs.twitch, 1000);
                PingReply discord = ping.Send(URLs.discord, 1000);
                long telegram = await Services.External.TelegramService.Ping();
                PingReply sevenTV = ping.Send(URLs.seventv, 1000);
                PingReply ISP = ping.Send("192.168.1.1", 1000);

                if (ISP.Status != IPStatus.Success)
                {
                    ISP = ping.Send("192.168.0.1", 1000);
                    if (ISP.Status != IPStatus.Success) Write("Twitch - Error ISP ping: " + ISP.Status.ToString(), "info", LogLevel.Warning);
                }
                #endregion
                #region Commands ping
                Stopwatch CommandExecute = Stopwatch.StartNew();

                string CommandName = "bot";
                List<string> CommandArguments = [];
                string CommandArgumentsAsString = "";

                UserData user = new()
                {
                    ID = "a123456789",
                    Language = "en",
                    Name = "test",
                    IsModerator = true,
                    IsBroadcaster = true
                };

                CommandData data = new()
                {
                    Name = CommandName.ToLower(),
                    UserID = "a123456789",
                    Arguments = CommandArguments,
                    ArgumentsString = CommandArgumentsAsString,
                    Channel = "test",
                    ChannelID = "a123456789",
                    MessageID = "a123456789",
                    Platform = PlatformsEnum.Telegram,
                    User = user,
                    TwitchArguments = new TwitchLib.Client.Events.OnChatCommandReceivedArgs(),
                    CommandInstanceID = Guid.NewGuid().ToString()
                };

                await Runner.Run(data, true);
                CommandExecute.Stop();
                #endregion
                #region DB ping
                Manager.CreateDatabase("DBPing.json");

                Stopwatch DBPing = Stopwatch.StartNew();
                Manager.Save("DBPing.json", "test", 123);
                DBPing.Stop();

                File.Delete("DBPing.json");
                #endregion
                #region MessageSaver ping
                Stopwatch MessageSaver = Stopwatch.StartNew();
                Message message = new()
                {
                    isMe = true,
                    messageDate = DateTime.Now,
                    messageText = "Wikipedia is a free online encyclopedia that is written and maintained by a community of volunteers, known as Wikipedians, " +
                    "through open collaboration and the wiki software MediaWiki. Founded by Jimmy Wales and Larry Sanger in 2001, Wikipedia has been hosted since " +
                    "2003 by the Wikimedia Foundation, an American nonprofit organization funded mainly by donations from readers. Wikipedia is the largest and " +
                    "most-read reference work in history.",
                    isModerator = true,
                    isSubscriber = true,
                    isPartner = true,
                    isStaff = true,
                    isTurbo = true,
                    isVip = true
                };
                MessagesWorker.SaveMessage("a123456789", "a123456789", message, PlatformsEnum.Telegram);

                MessageSaver.Stop();

                File.Delete(Path.Combine(Engine.Bot.Pathes.Users, "TELEGRAM", "a123456789.json"));
                Directory.Delete(Path.Combine(Engine.Bot.Pathes.Channels, "TELEGRAM", "a123456789"), true);
                #endregion
                #region Local ping
                Stopwatch LocalPing = Stopwatch.StartNew();
                Engine.Ping();
                LocalPing.Stop();
                #endregion

                decimal cpuPercent = Engine.TelemetryCPUItems == 0 ? 0 : Engine.TelemetryCPU / Engine.TelemetryCPUItems;
                decimal tpsAverage = Engine.TelemetryTPSItems == 0 ? 0 : (decimal)Engine.TelemetryTPS / Engine.TelemetryTPSItems;
                float coinCurrency = Engine.Coins == 0 ? 0 : Engine.BankDollars / Engine.Coins;

                Engine.TelemetryCPU = 0;
                Engine.TelemetryCPUItems = 0;
                Engine.TelemetryTPS = 0;
                Engine.TelemetryTPSItems = 0;
                Start.Stop();

                await Task.Delay(500);
                long memory = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);

                Chat.TwitchSend(BotName.ToLower(), $"/me glorp 📡 ᴛᴇʟᴇᴍᴇᴛʀʏ №1 | " +
                    $"🏃‍♂️ Work time: {DateTime.Now - Engine.StartTime:dd\\:hh\\:mm\\.ss} | " +
                    $"📲 Memory: {memoryBefore}Mbyte → {memory}Mbyte | " +
                    $"🔋 Battery: {Battery.GetBatteryCharge()}% ({Battery.IsCharging()}) | " +
                    $"⚡ CPU: {cpuPercent:0.00}% | " +
                    $"⌛ TPS: {tpsAverage:0.00} | " +
                    $"⌚ TT: {Engine.TicksCounter} | " +
                    $"⚠ ST: {Engine.SkippedTicks} | " +
                    $"👾 DankDB: {cacheItemsBefore} → {Worker.cache.count} | " +
                    $"🤨 Emotes: {EmotesCache.Count} | " +
                    $"📺 7tv: {ChannelsSevenTVEmotes.Count} | " +
                    $"🤔 Emote sets: {EmoteSetsCache.Count} | " +
                    $"🔍 7tv USC: {UsersSearchCache.Count} | " +
                    $"💬 Messages: {MessagesProccessed} | " +
                    $"🤖 Discord: {Clients.Discord.Guilds.Count} | " +
                    $"📋 Commands: {Engine.CompletedCommands} | " +
                    $"👥 Users: {Engine.Users}", "", "", "", true, false);

                await Task.Delay(500);
                Chat.TwitchSend(BotName.ToLower(), $"/me glorp 📡 ᴛᴇʟᴇᴍᴇᴛʀʏ №2 | " +
                    $"Twitch: {twitch.RoundtripTime}ms | " +
                    $"Discord: {discord.RoundtripTime}ms | " +
                    $"Telegram: {telegram}ms | " +
                    $"7tv: {sevenTV.RoundtripTime}ms | " +
                    $"🚄 ISP: {ISP.RoundtripTime}ms | " +
                    $"{Engine.Bot.CoinSymbol} Coins: {Engine.Coins:0.00} | " +
                    $"Coin currency: ${coinCurrency:0.00000000} | " +
                    $"Commands ping: {CommandExecute.ElapsedMilliseconds}ms | " +
                    $"DB ping: {DBPing.ElapsedMilliseconds}ms | " +
                    $"MessageSaver ping: {MessageSaver.ElapsedMilliseconds}ms | " +
                    $"Local ping: {LocalPing.ElapsedMilliseconds}ms", "", "", "", true, false);

                Write($"Twitch - Telemetry ended! ({Start.ElapsedMilliseconds}ms)", "telemetry");

                try
                {
                    var newToken = await RefreshAccessToken(Tokens.Twitch);
                    if (newToken != null) Tokens.Twitch = newToken;
                }
                catch (Exception ex)
                {
                    Write(ex);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Restarts the bot instance gracefully.
        /// </summary>
        [ConsoleSector("butterBror.InternalBot", "Restart")]
        public void Restart()
        {
            FunctionsUsed.Add();

            Write("Restarting...", "info");

            Initialized = false;
            Connected = false;
            Task.Delay(5000);
            Engine.Exit(1);
        }

        /// <summary>
        /// Shuts down the bot instance cleanly.
        /// </summary>
        /// <remarks>
        /// - Sends shutdown message to Twitch channels
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "TurnOff")]
        public void TurnOff()
        {
            FunctionsUsed.Add();

            try
            {
                Clients.Twitch.SendMessage(BotName, "Zevlo Turning off...");

                foreach (var channel in TwitchConnectAnnounce)
                {
                    try
                    {
                        Clients.Twitch.SendMessage(Names.GetUsername(channel, PlatformsEnum.Twitch), "Zevlo Turning off...");
                    }
                    catch { }
                }
                Initialized = false;
                Connected = false;

                Write($"Bot is disabled!", "info");
                Task.Delay(1000).Wait();
                Engine.Exit(0);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Initializes default settings file with initial configuration.
        /// </summary>
        /// <param name="path">Path where to create the settings file.</param>
        /// <remarks>
        /// - Creates empty settings file if missing
        /// - Sets up default values for all bot parameters
        /// - Preserves existing settings if file exists
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "InitializeSettingsFile")]
        private void InitializeSettingsFile(string path)
        {
            FunctionsUsed.Add();

            FileUtil.CreateFile(path);
            SafeManager.Save(path, "bot_name", "", false);
            SafeManager.Save(path, "discord_token", "", false);
            SafeManager.Save(path, "imgur_token", "", false);
            SafeManager.Save(path, "user_id", "", false);
            SafeManager.Save(path, "client_id", "", false);
            SafeManager.Save(path, "twitch_secret_token", "", false);
            SafeManager.Save(path, "twitch_connect_message_channels", Array.Empty<string>(), false);
            SafeManager.Save(path, "twitch_reconnect_message_channels", Array.Empty<string>(), false);
            SafeManager.Save(path, "twitch_connect_channels", new[] { "First channel", "Second channel" }, false);
            string[] apis = { "First api", "Second api" };
            SafeManager.Save(path, "weather_token", apis, false);
            SafeManager.Save(path, "gpt_tokens", apis, false);
            SafeManager.Save(path, "telegram_token", "", false);
            SafeManager.Save(path, "twitch_version_message_channels", Array.Empty<string>(), false);
            SafeManager.Save(path, "7tv_token", "", false);
            SafeManager.Save(path, "coin_symbol", "\U0001f96a", false);
            SafeManager.Save(path, "currency_mentioned_payment", 8, false);
            SafeManager.Save(path, "currency_mentioner_payment", 2);
        }

        /// <summary>
        /// Loads bot configuration from settings file into memory.
        /// </summary>
        /// <remarks>
        /// - Reads all critical configuration values
        /// - Sets up tokens, connection strings, and bot behavior parameters
        /// - Initializes Twitch token manager with loaded credentials
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "LoadSettings")]
        private void LoadSettings()
        {
            FunctionsUsed.Add();

            BotName = Manager.Get<string>(Pathes.Settings, "bot_name");
            TwitchChannels = Manager.Get<string[]>(Pathes.Settings, "twitch_connect_channels");
            TwitchReconnectAnnounce = Manager.Get<string[]>(Pathes.Settings, "twitch_reconnect_message_channels");
            TwitchConnectAnnounce = Manager.Get<string[]>(Pathes.Settings, "twitch_connect_message_channels");
            Tokens.Discord = Manager.Get<string>(Pathes.Settings, "discord_token");
            Tokens.Imgur = Manager.Get<string>(Pathes.Settings, "imgur_token");
            TwitchClientId = Manager.Get<string>(Pathes.Settings, "client_id");
            Tokens.TwitchSecretToken = Manager.Get<string>(Pathes.Settings, "twitch_secret_token");
            Tokens.Telegram = Manager.Get<string>(Pathes.Settings, "telegram_token");
            TwitchNewVersionAnnounce = Manager.Get<string[]>(Pathes.Settings, "twitch_version_message_channels");
            Tokens.SevenTV = Manager.Get<string>(Pathes.Settings, "7tv_token");
            UsersSevenTVIDs = Manager.Get<Dictionary<string, string>>(Pathes.SevenTVCache, "Ids");
            CoinSymbol = Manager.Get<string>(Pathes.Settings, "coin_symbol");
            CurrencyMentioned = Manager.Get<int>(Pathes.Settings, "currency_mentioned_payment");
            CurrencyMentioner = Manager.Get<int>(Pathes.Settings, "currency_mentioner_payment");
            Executor = Convert.ToChar(Manager.Get<string>(Pathes.Settings, "executor"));
        }

        /// <summary>
        /// Persists current emote cache data to storage.
        /// </summary>
        /// <remarks>
        /// - Serializes emote cache dictionaries to JSON
        /// - Saves to predefined cache file path
        /// - Handles directory creation if needed
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "SaveEmoteCache")]
        public void SaveEmoteCache()
        {
            FunctionsUsed.Add();

            try
            {
                var data = new
                {
                    Channels7tvEmotes = ChannelsSevenTVEmotes.ToDictionary(kv => kv.Key, kv => kv.Value),
                    EmoteSetCache = EmoteSetsCache.ToDictionary(kv => kv.Key, kv => kv.Value),
                    UserSearchCache = UsersSearchCache.ToDictionary(kv => kv.Key, kv => kv.Value),
                    EmoteCache = EmotesCache.ToDictionary(kv => kv.Key, kv => kv.Value)
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(Pathes.SevenTVCache));
                FileUtil.SaveFileContent(Pathes.SevenTVCache, json);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Loads previously saved emote cache data from storage.
        /// </summary>
        /// <remarks>
        /// - Deserializes emote cache from JSON file
        /// - Populates all emote-related dictionaries
        /// - Handles missing cache file gracefully
        /// </remarks>
        [ConsoleSector("butterBror.InternalBot", "LoadEmoteCache")]
        public void LoadEmoteCache()
        {
            FunctionsUsed.Add();

            try
            {
                if (!FileUtil.FileExists(Pathes.SevenTVCache)) return;

                string json = FileUtil.GetFileContent(Pathes.SevenTVCache);
                var template = new
                {
                    Channels7tvEmotes = new Dictionary<string, (List<string> emotes, DateTime expiration)>(),
                    EmoteSetCache = new Dictionary<string, (string setId, DateTime expiration)>(),
                    UserSearchCache = new Dictionary<string, (string userId, DateTime expiration)>(),
                    EmoteCache = new Dictionary<string, (SevenTV.Types.Rest.Emote emote, DateTime expiration)>()
                };

                var data = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(json, template);

                ChannelsSevenTVEmotes = new ConcurrentDictionary<string, (List<string>, DateTime)>(data.Channels7tvEmotes);
                EmoteSetsCache = new ConcurrentDictionary<string, (string, DateTime)>(data.EmoteSetCache);
                UsersSearchCache = new ConcurrentDictionary<string, (string, DateTime)>(data.UserSearchCache);
                EmotesCache = new ConcurrentDictionary<string, (SevenTV.Types.Rest.Emote, DateTime)>(data.EmoteCache);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
