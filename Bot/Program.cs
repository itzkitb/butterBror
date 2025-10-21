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
            Core.Bot.Logger.Write(":alienPls: Loading...");

            Core.Bot.Logger.Write(@$"🚀 Executed command ""test"":
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


            // Register all services
            services.AddSingleton<ClientService>();

            services.AddSingleton<PathService>(provider => {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SillyApps/");
                Core.Bot.Logger.Write($"Root path: {path}", Core.Bot.Logger.LogLevel.Debug);
                return new PathService(path);
            });
            services.AddSingleton<Tokens>();
            services.AddSingleton<SQLService>();
            services.AddSingleton<EmoteCacheService>();
            services.AddSingleton<SettingsService>(provider => {
                string path = provider.GetRequiredService<PathService>().Settings;
                Core.Bot.Logger.Write($"Settings path: {path}", Core.Bot.Logger.LogLevel.Debug);
                return new SettingsService(path);
            });
            services.AddSingleton<SevenTvService>(provider => {
                return new SevenTvService(client);
            });
            services.AddSingleton<AIService>();
            services.AddSingleton<YouTubeService>(provider => {
                return new YouTubeService(client);
            });
            services.AddSingleton<CooldownManager>();
            services.AddSingleton<CurrencyManager>();
            services.AddSingleton<MessageProcessor>();
            services.AddSingleton<PlatformMessageSender>();
            services.AddSingleton<CommandService>();
            services.AddSingleton<Executor>();
            services.AddSingleton<Runner>();
            services.AddSingleton<BlockedWordDetector>();

            // Register Bot itself
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
            var serviceProvider = services.BuildServiceProvider();

            // Resolve and start the bot
            BotInstance = serviceProvider.GetRequiredService<BotInstance>();
            BotInstance.Start(args);
        }
    }
}