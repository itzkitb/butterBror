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
            System.Console.Title = "Loading libraries...";
            System.Console.WriteLine(":alienPls: Loading libraries...");

            // Set up DI container
            var services = new ServiceCollection();

            // Register all services (we'll add more below)
            services.AddSingleton<ClientService>();
            services.AddSingleton<PathService>();
            services.AddSingleton<Tokens>();
            services.AddSingleton<SQLService>();
            services.AddSingleton<EmoteCacheService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<SevenTvService>();
            services.AddSingleton<AIService>();
            services.AddSingleton<ImgurService>();
            services.AddSingleton<YouTubeService>();
            services.AddSingleton<CooldownManager>();
            services.AddSingleton<CurrencyManager>();
            services.AddSingleton<MessageProcessor>();
            services.AddSingleton<PlatformMessageSender>();
            services.AddSingleton<CommandService>();
            services.AddSingleton<Executor>();
            services.AddSingleton<Runner>();
            services.AddSingleton<Sender>();

            // Register Bot itself
            services.AddSingleton<BotInstance>();

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Resolve and start the bot
            BotInstance = serviceProvider.GetRequiredService<BotInstance>();
            BotInstance.Start(args);
        }
    }
}