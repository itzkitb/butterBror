// Program.cs
using bb.Core.Commands;
using bb.Core.Configuration;
using bb.Core.Services;
using bb.Models.SevenTVLib;
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
            Core.Bot.Console.Write(":alienPls: Loading...");

            Core.Bot.Console.Write(@$"🚀 Executed command ""test"":
- User: TestPlatform/1234567890
- Arguments: """"
- Location: ""Test/1234567890"" (ChatID: 1234567890)
- Balance: 12345.67
- Completed in: 10ms
- Blocked words detected: ❎ in 15,8946ms
- Blocked word: Blocked word: NotFound/""Empty""");

            // Set up DI container
            var services = new ServiceCollection();

            HttpClient client = new HttpClient();

            var initialize = Core.Bot.Console.Progress("Initializing clients...", 0, 19);

            // Register all services
            services.AddSingleton<ClientService>();

            Core.Bot.Console.UpdateProgress(initialize, 1, 19, "Initializing paths...");
            services.AddSingleton<PathService>(provider => {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SillyApps/");
                Core.Bot.Console.Write($"Root path: {path}", Core.Bot.Console.LogLevel.Debug);
                return new PathService(path);
            });

            Core.Bot.Console.UpdateProgress(initialize, 2, 19, "Initializing tokens...");
            services.AddSingleton<Tokens>();

            Core.Bot.Console.UpdateProgress(initialize, 3, 19, "Initializing SQL...");
            services.AddSingleton<SQLService>();

            Core.Bot.Console.UpdateProgress(initialize, 4, 19, "Initializing emote cache...");
            services.AddSingleton<EmoteCacheService>();

            Core.Bot.Console.UpdateProgress(initialize, 5, 19, "Initializing settings...");
            services.AddSingleton<SettingsService>(provider => {
                string path = provider.GetRequiredService<PathService>().Settings;
                Core.Bot.Console.Write($"Settings path: {path}", Core.Bot.Console.LogLevel.Debug);
                return new SettingsService(path);
            });

            Core.Bot.Console.UpdateProgress(initialize, 6, 19, "Initializing 7tv...");
            services.AddSingleton<SevenTvService>(provider => {
                return new SevenTvService(client);
            });

            Core.Bot.Console.UpdateProgress(initialize, 7, 19, "Initializing AI...");
            services.AddSingleton<AIService>();

            Core.Bot.Console.UpdateProgress(initialize, 8, 19, "Initializing YouTube...");
            services.AddSingleton<YouTubeService>(provider => {
                return new YouTubeService(client);
            });

            Core.Bot.Console.UpdateProgress(initialize, 9, 19, "Initializing cooldown...");
            services.AddSingleton<CooldownManager>();

            Core.Bot.Console.UpdateProgress(initialize, 10, 19, "Initializing currency...");
            services.AddSingleton<CurrencyManager>();

            Core.Bot.Console.UpdateProgress(initialize, 11, 19, "Initializing message processor...");
            services.AddSingleton<MessageProcessor>();

            Core.Bot.Console.UpdateProgress(initialize, 12, 19, "Initializing sender...");
            services.AddSingleton<PlatformMessageSender>();

            Core.Bot.Console.UpdateProgress(initialize, 13, 19, "Initializing command service...");
            services.AddSingleton<CommandService>();

            Core.Bot.Console.UpdateProgress(initialize, 14, 19, "Initializing command executor...");
            services.AddSingleton<Executor>();

            Core.Bot.Console.UpdateProgress(initialize, 15, 19, "Initializing command runner...");
            services.AddSingleton<Runner>();

            Core.Bot.Console.UpdateProgress(initialize, 16, 19, "Initializing blocked word detector...");
            services.AddSingleton<BlockedWordDetector>();

            // Register Bot itself
            Core.Bot.Console.UpdateProgress(initialize, 17, 19, "Initializing bot...");
            services.AddSingleton<BotInstance>();

            services.AddSingleton<IGitHubActionsNotifier>(provider =>
            {
                return new GitHubActionsNotifier(
                    repo: "itzkitb/butterBror",
                    token: provider.GetRequiredService<SettingsService>().Get<string>("github_token"),
                    pollingInterval: TimeSpan.FromMinutes(1)
                );
            });
            services.AddHostedService(provider => provider.GetRequiredService<IGitHubActionsNotifier>());

            // Build the service provider
            Core.Bot.Console.UpdateProgress(initialize, 18, 19, "Building the service provider...");
            var serviceProvider = services.BuildServiceProvider();

            // Resolve and start the bot
            Core.Bot.Console.UpdateProgress(initialize, 19, 19, "Starting...");
            BotInstance = serviceProvider.GetRequiredService<BotInstance>();
            BotInstance.Start(args);
        }
    }
}