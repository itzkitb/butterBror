using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Things;
using butterBror.Utils.Tools;
using butterBror.Utils.Tools.Device;
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
using static butterBror.Core.Statistics;
using static butterBror.Utils.Things.Console;
using static butterBror.Utils.Tools.TwitchToken;

namespace butterBror
{
    public class InternalBot
    {
        internal ClientWorker Clients = new ClientWorker();
        public PathWorker Pathes = new PathWorker();
        internal Tokens Tokens = new Tokens();

        public bool TwitchReconnected = false;
        public bool Initialized = false;
        public bool NeedRestart = false;
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
        private DateTime StartTime = DateTime.UtcNow;


        public readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(30);

        [ConsoleSector("butterBror.InternalBot", "Start")]
        public async void Start(int ThreadID)
        {
            FunctionsUsed.Add();

            StartTime = DateTime.UtcNow;
            Thread.CurrentThread.Name = ThreadID.ToString();
            Core.Ready = false;

            if (FileUtil.FileExists(Pathes.Currency))
            {
                Core.Coins = Manager.Get<float>(Pathes.Currency, "totalAmount");
                Core.BankDollars = Manager.Get<int>(Pathes.Currency, "totalDollarsInTheBank");
                Core.Users = Manager.Get<int>(Pathes.Currency, "totalUsers");
            }

            try
            {
                Write("Checking directories right now...", "info");
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


                Write("Checking files right now...", "info");
                if (!FileUtil.FileExists(Pathes.Settings))
                {
                    InitializeSettingsFile(Pathes.Settings);
                    Write($"The settings file has been created! ({Pathes.Settings})", "info");
                    Thread.Sleep(-1);
                }

                string[] files = {
                            Pathes.Cookies, Pathes.BlacklistWords, Pathes.BlacklistReplacements,
                            Pathes.Currency, Pathes.Cache, Pathes.Logs, Pathes.APIUses,
                            Path.Combine(Pathes.TranslateDefault, "ru.json"),
                            Path.Combine(Pathes.TranslateDefault, "en.json"), Path.Combine(Pathes.Main, "VERSION.txt")
                    };

                Write("Creating files...", "info");
                foreach (var file in files)
                {
                    FileUtil.CreateFile(file);
                }

                Core.PreviousVersion = File.ReadAllText(Path.Combine(Pathes.Main, "VERSION.txt"));
                File.WriteAllText(Path.Combine(Pathes.Main, "VERSION.txt"), $"{Core.Version}.{Core.Patch}");

                Commands.IndexCommands();

                Write("Loading settigns...", "info");
                LoadSettings();

                Tokens.TwitchGetter = new(TwitchClientId, Tokens.TwitchSecretToken, Pathes.Main + "TWITCH_AUTH.json");
                var token = await TwitchToken.GetTokenAsync();

                if (token != null)
                {
                    Tokens.Twitch = token;
                    Connect();
                }
                else
                {
                    Write("Twitch token is null! Something went wrong...", "info");
                    Restart();
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                Restart();
            }
        }

        [ConsoleSector("butterBror.InternalBot", "Connect")]
        public async Task Connect()
        {
            FunctionsUsed.Add();

            try
            {
                Write("Connecting to Twitch...", "info");
                ConnectToTwitch();

                Write("Connecting to Discord...", "info");
                await ConnectToDiscord();

                Write("Connecting to Telegram...", "info");
                ConnectToTelegram();

                Write("Loading 7tv cache...", "info");
                LoadEmoteCache();

                DateTime endTime = DateTime.UtcNow;
                Core.Ready = true;
                Initialized = true;
                Connected = true;
                Write($"Well done! ({(endTime - StartTime).TotalMilliseconds} ms)", "info");
            }
            catch (Exception ex)
            {
                Write(ex);
                Restart();
            }
        }

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
            Clients.Twitch.OnChatCommandReceived += Commands.Twitch;
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
                var channel2 = Names.GetUsername(channel, Platforms.Twitch);
                if (channel2 == null) not_founded_channels.Add(channel);
                return channel2;
            }).Where(channel => channel != "NONE\n"));

            Write($"Twitch - Connecting to {send_channels}", "info");
            foreach (var channel in TwitchChannels)
            {
                var channel2 = Names.GetUsername(channel, Platforms.Twitch);
                if (channel2 != null) Clients.Twitch.JoinChannel(channel2);
            }
            foreach (var channel in not_founded_channels)
                Write("Twitch - Can't find ID for " + channel, "info", LogLevel.Warning);

            Clients.Twitch.JoinChannel(BotName.ToLower());
            Clients.Twitch.SendMessage(BotName.ToLower(), "truckCrash Connecting to twitch...");
        }

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

            Clients.Discord.Log += DiscordEvents.LogAsync;
            Clients.Discord.JoinedGuild += DiscordEvents.ConnectToGuilt;
            Clients.Discord.Ready += DiscordWorker.ReadyAsync;
            Clients.Discord.MessageReceived += DiscordWorker.MessageReceivedAsync;
            Clients.Discord.SlashCommandExecuted += DiscordEvents.SlashCommandHandler;
            Clients.Discord.ApplicationCommandCreated += DiscordEvents.ApplicationCommandCreated;
            Clients.Discord.ApplicationCommandDeleted += DiscordEvents.ApplicationCommandDeleted;
            Clients.Discord.ApplicationCommandUpdated += DiscordEvents.ApplicationCommandUpdated;
            Clients.Discord.ChannelCreated += DiscordEvents.ChannelCreated;
            Clients.Discord.ChannelDestroyed += DiscordEvents.ChannelDeleted;
            Clients.Discord.ChannelUpdated += DiscordEvents.ChannelUpdated;
            Clients.Discord.Connected += DiscordEvents.Connected;
            Clients.Discord.ButtonExecuted += DiscordEvents.ButtonTouched;

            await DiscordWorker.RegisterCommandsAsync();
            await Clients.Discord.LoginAsync(TokenType.Bot, Tokens.Discord);
            await Clients.Discord.StartAsync();
        }

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

            Clients.Telegram.StartReceiving(Utils.Workers.Telegram.UpdateHandler, Utils.Workers.Telegram.ErrorHandler, TelegramReceiverOptions, Clients.TelegramCancellationToken.Token);
        }

        [ConsoleSector("butterBror.InternalBot", "StatusSender")]
        public async Task SendTelemetry()
        {
            FunctionsUsed.Add();

            try
            {
                Write("Twitch - Telemetry started!", "info");
                Chat.TwitchSend(BotName.ToLower(), $"glorp 📡 ᴛᴇʟᴇᴍᴇᴛʀʏ sᴛᴀʀᴛᴇᴅ...", "", "", "", true, false);
                Stopwatch Start = Stopwatch.StartNew();

                int cacheItemsBefore = Worker.cache.count;
                long memoryBefore = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
                Worker.cache.Clear(TimeSpan.FromMinutes(10));

                #region Ethernet ping
                Ping ping = new();
                PingReply twitch = ping.Send(URLs.twitch, 1000);
                PingReply discord = ping.Send(URLs.discord, 1000);
                long telegram = await Utils.Tools.API.Telegram.Ping();
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
                    id = "a123456789",
                    language = "en",
                    username = "test",
                    channel_moderator = true,
                    channel_broadcaster = true
                };

                CommandData data = new()
                {
                    name = CommandName.ToLower(),
                    user_id = "a123456789",
                    arguments = CommandArguments,
                    arguments_string = CommandArgumentsAsString,
                    channel = "test",
                    channel_id = "a123456789",
                    message_id = "a123456789",
                    platform = Platforms.Telegram,
                    user = user,
                    twitch_arguments = new TwitchLib.Client.Events.OnChatCommandReceivedArgs(),
                    command_instance_id = Guid.NewGuid().ToString()
                };

                await Commands.Run(data, true);
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
                MessagesWorker.Message message = new()
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
                MessagesWorker.SaveMessage("a123456789", "a123456789", message, Platforms.Telegram);

                MessageSaver.Stop();

                File.Delete(Path.Combine(Core.Bot.Pathes.Users, "TELEGRAM", "a123456789.json"));
                Directory.Delete(Path.Combine(Core.Bot.Pathes.Channels, "TELEGRAM", "a123456789"), true);
                #endregion
                #region Local ping
                Stopwatch LocalPing = Stopwatch.StartNew();
                Core.Ping();
                LocalPing.Stop();
                #endregion

                decimal cpuPercent = Core.CPUCounterItems == 0 ? 0 : Core.CPUCounter / Core.CPUCounterItems;
                decimal tpsAverage = Core.TPSCounterItems == 0 ? 0 : (decimal)Core.TPSCounter / Core.TPSCounterItems;
                float coinCurrency = Core.Coins == 0 ? 0 : Core.BankDollars / Core.Coins;

                Core.CPUCounter = 0;
                Core.CPUCounterItems = 0;
                Core.TPSCounter = 0;
                Core.TPSCounterItems = 0;
                Start.Stop();

                await Task.Delay(500);
                long memory = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);

                Chat.TwitchSend(BotName.ToLower(), $"/me glorp 📡 ᴛᴇʟᴇᴍᴇᴛʀʏ №1 | " +
                    $"🏃‍♂️ Work time: {DateTime.Now - Core.StartTime:dd\\:hh\\:mm\\.ss} | " +
                    $"📲 Memory: {memoryBefore}Mbyte → {memory}Mbyte | " +
                    $"🔋 Battery: {Battery.GetBatteryCharge()}% ({Battery.IsCharging()}) | " +
                    $"⚡ CPU: {cpuPercent:0.00}% | " +
                    $"⌛ TPS: {tpsAverage:0.00} | " +
                    $"⌚ TT: {Core.TicksCounter} | " +
                    $"⚠ ST: {Core.SkippedTicks} | " +
                    $"👾 DankDB: {cacheItemsBefore} → {Worker.cache.count} | " +
                    $"🤨 Emotes: {EmotesCache.Count} | " +
                    $"📺 7tv: {ChannelsSevenTVEmotes.Count} | " +
                    $"🤔 Emote sets: {EmoteSetsCache.Count} | " +
                    $"🔍 7tv USC: {UsersSearchCache.Count} | " +
                    $"💬 Messages: {MessagesProccessed} | " +
                    $"🤖 Discord: {Clients.Discord.Guilds.Count} | " +
                    $"📋 Commands: {Core.CompletedCommands} | " +
                    $"👥 Users: {Core.Users}", "", "", "", true, false);

                await Task.Delay(500);
                Chat.TwitchSend(BotName.ToLower(), $"/me glorp 📡 ᴛᴇʟᴇᴍᴇᴛʀʏ №2 | " +
                    $"Twitch: {twitch.RoundtripTime}ms | " +
                    $"Discord: {discord.RoundtripTime}ms | " +
                    $"Telegram: {telegram}ms | " +
                    $"7tv: {sevenTV.RoundtripTime}ms | " +
                    $"🚄 ISP: {ISP.RoundtripTime}ms | " +
                    $"{Core.Bot.CoinSymbol} Coins: {Core.Coins:0.00} | " +
                    $"Coin currency: ${coinCurrency:0.00000000} | " +
                    $"Commands ping: {CommandExecute.ElapsedMilliseconds}ms | " +
                    $"DB ping: {DBPing.ElapsedMilliseconds}ms | " +
                    $"MessageSaver ping: {MessageSaver.ElapsedMilliseconds}ms | " +
                    $"Local ping: {LocalPing.ElapsedMilliseconds}ms", "", "", "", true, false);

                Write($"Twitch - Telemetry ended! ({Start.ElapsedMilliseconds}ms)", "info");

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

        [ConsoleSector("butterBror.InternalBot", "Restart")]
        public void Restart()
        {
            FunctionsUsed.Add();

            Write("Restarting...", "info");
            Disconnect();
        }

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
                        Clients.Twitch.SendMessage(Names.GetUsername(channel, Platforms.Twitch), "Zevlo Turning off...");
                    }
                    catch { }
                }

                Disconnect();
                Write($"Bot is disabled!", "info");
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.InternalBot", "Disconnect")]
        private void Disconnect()
        {
            FunctionsUsed.Add();

            try
            {
                foreach (var channel in Clients.Twitch.JoinedChannels)
                {
                    try
                    {
                        Clients.Twitch.LeaveChannel(channel);
                    }
                    catch (Exception ex)
                    {
                        Write($"Twitch - Leave channel error: {ex.Message} : {ex.StackTrace}", "info", LogLevel.Warning);
                    }
                }

                Clients.Twitch.Disconnect();
                Clients.TelegramCancellationToken.Dispose();
                Clients.Discord.Dispose();
            }
            catch (Exception ex)
            {
                Write(ex);
            }

            NeedRestart = true;
            Initialized = false;
            Connected = false;
        }

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
