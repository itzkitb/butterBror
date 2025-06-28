using butterBror.Utils;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using static butterBror.Utils.Things.Console;
using butterBror.Utils.Tools;

namespace butterBror.Utils.Workers
{
    public class Discord
    {
        [ConsoleSector("butterBror.Utils.Workers.Discord", "ReadyAsync")]
        public static async Task ReadyAsync()
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Discord - Connected as {Core.Bot.Clients.Discord.CurrentUser}!", "info");

                foreach (var guild in Core.Bot.Clients.Discord.Guilds)
                {
                    Core.Bot.DiscordServers++;
                }

                Write($"Discord - Connected to {Core.Bot.DiscordServers} servers", "info");
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Workers.Discord", "MessageReceivedAsync")]
        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (!(message is SocketUserMessage msg) || message.Author.IsBot) return;
                OnMessageReceivedArgs e = default;
                await Command.ProcessMessageAsync(message.Author.Id.ToString(), ((SocketGuildChannel)message.Channel).Guild.Id.ToString(), message.Author.Username.ToLower(), message.Content, e, ((SocketGuildChannel)message.Channel).Guild.Name, Platforms.Discord, null, message.Channel.ToString());

                if (message.Content.StartsWith(Core.Bot.Executor))
                {
                    Commands.Discord(message);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Workers.Discord", "RegisterCommandsAsync")]
        public static async Task RegisterCommandsAsync()
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Core.Bot.Clients.Discord.Ready += RegisterSlashCommands;
                Core.Bot.Clients.Discord.MessageReceived += DiscordEvents.HandleCommandAsync;
                await Core.Bot.DiscordCommandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: Core.Bot.DiscordServiceProvider);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Workers.Discord", "RegisterSlashCommands")]
        private static async Task RegisterSlashCommands()
        {
            Core.Statistics.FunctionsUsed.Add();

            try
            {
                Write("Discord - Updating commands...", "info");
                await Core.Bot.Clients.Discord.Rest.DeleteAllGlobalCommandsAsync();

                await Core.Bot.Clients.Discord.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                    .WithName("ping")
                    .WithDescription("Check bot status")
                    .Build());
                await Core.Bot.Clients.Discord.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                    .WithName("status")
                    .WithDescription("View the bot's status. (Bot administrators only)")
                    .Build());
                await Core.Bot.Clients.Discord.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                    .WithName("weather")
                    .WithDescription("Check the weather")
                    .AddOption("location", ApplicationCommandOptionType.String, "weather check location", isRequired: false)
                    .AddOption("showpage", ApplicationCommandOptionType.Integer, "show weather on page", isRequired: false)
                    .AddOption("page", ApplicationCommandOptionType.Integer, "show the result page of the received weather", isRequired: false)
                    .Build());

                Write("Discord - All commands are registered!", "info");
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
