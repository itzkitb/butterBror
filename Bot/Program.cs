// Program.cs
using bb.Core.Commands;
using bb.Core.Configuration;
using bb.Core.Services;
using bb.Services.External;
using bb.Utils;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace bb
{
    public static class Program
    {
        public static BotInstance BotInstance;

        public static void Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            System.Console.Title = "Loading...";
            System.Console.WriteLine(":alienPls: Loading...");

            // Set up DI container
            var services = new ServiceCollection();

            HttpClient client = new HttpClient();

            // Register all services
            Core.Bot.Console.Write("Initializing client...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<ClientService>();
            Core.Bot.Console.Write("Initializing paths...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<PathService>(provider => {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SillyApps/");
                Core.Bot.Console.Write($"Root path: {path}", Core.Bot.Console.LogLevel.Debug);
                return new PathService(path);
            });
            Core.Bot.Console.Write("Initializing tokens...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<Tokens>();
            Core.Bot.Console.Write("Initializing SQL...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<SQLService>();
            Core.Bot.Console.Write("Initializing emote cache...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<EmoteCacheService>();
            Core.Bot.Console.Write("Initializing settings...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<SettingsService>(provider => {
                string path = provider.GetRequiredService<PathService>().Settings;
                Core.Bot.Console.Write($"Settings path: {path}", Core.Bot.Console.LogLevel.Debug);
                return new SettingsService(path);
            });
            Core.Bot.Console.Write("Initializing 7tv...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<SevenTvService>(provider => {
                return new SevenTvService(client);
            });
            Core.Bot.Console.Write("Initializing AI...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<AIService>();
            Core.Bot.Console.Write("Initializing YT...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<YouTubeService>(provider => {
                return new YouTubeService(client);
            });
            Core.Bot.Console.Write("Initializing cooldown...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<CooldownManager>();
            Core.Bot.Console.Write("Initializing currency...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<CurrencyManager>();
            Core.Bot.Console.Write("Initializing message processor...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<MessageProcessor>();
            Core.Bot.Console.Write("Initializing sender...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<PlatformMessageSender>();
            Core.Bot.Console.Write("Initializing command service...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<CommandService>();
            Core.Bot.Console.Write("Initializing command executor...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<Executor>();
            Core.Bot.Console.Write("Initializing command runner...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<Runner>();
            Core.Bot.Console.Write("Initializing blocked word detector...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<BlockedWordDetector>();

            // Register Bot itself
            Core.Bot.Console.Write("Initializing bot...", Core.Bot.Console.LogLevel.Debug);
            services.AddSingleton<BotInstance>();

            // Build the service provider
            Core.Bot.Console.Write("Building the service provider...", Core.Bot.Console.LogLevel.Debug);
            var serviceProvider = services.BuildServiceProvider();

            // Resolve and start the bot
            Core.Bot.Console.Write("Starting...", Core.Bot.Console.LogLevel.Debug);
            BotInstance = serviceProvider.GetRequiredService<BotInstance>();
            BotInstance.Start(args);
        }
    }
}