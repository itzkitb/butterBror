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
using static butterBror.Utils.Bot.Console;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror.Utils.Workers
{
    /// <summary>
    /// Provides event handlers and command registration for Discord API interactions.
    /// </summary>
    public partial class DiscordWorker
    {
        /// <summary>
        /// Handles the bot's ready event when connected to Discord.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// - Logs successful connection with bot username
        /// - Updates server count statistics
        /// - Handles connection errors internally
        /// </remarks>
        [ConsoleSector("butterBror.Utils.DiscordWorker", "ReadyAsync")]
        public static async Task ReadyAsync()
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Discord - Connected as {Engine.Bot.Clients.Discord.CurrentUser}!", "info");
                Engine.Bot.DiscordServers = (ulong)Engine.Bot.Clients.Discord.Guilds.Count;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Processes incoming Discord messages for command handling and chat processing.
        /// </summary>
        /// <param name="message">The received message from Discord.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// - Skips messages from bots or invalid channel types
        /// - Routes messages to command processing system
        /// - Handles prefix-based command detection
        /// - Integrates with chat processing and AFK systems
        /// </remarks>
        [ConsoleSector("butterBror.Utils.DiscordWorker", "MessageReceivedAsync")]
        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                if (!(message is SocketUserMessage msg) || message.Author.IsBot) return;
                OnMessageReceivedArgs e = default;
                await Command.ProcessMessageAsync(message.Author.Id.ToString(), ((SocketGuildChannel)message.Channel).Guild.Id.ToString(), message.Author.Username.ToLower(), message.Content, e, ((SocketGuildChannel)message.Channel).Guild.Name, Platforms.Discord, null, message.Channel.ToString());

                if (message.Content.StartsWith(Engine.Bot.Executor))
                {
                    Commands.Discord(message);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Registers Discord slash commands and event handlers.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// - Sets up global command handlers
        /// - Registers text-based command handlers
        /// - Loads command modules from assembly
        /// - Handles command registration errors
        /// </remarks>
        [ConsoleSector("butterBror.Utils.DiscordWorker", "RegisterCommandsAsync")]
        public static async Task RegisterCommandsAsync()
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                Engine.Bot.Clients.Discord.Ready += RegisterSlashCommands;
                Engine.Bot.Clients.Discord.MessageReceived += DiscordEvents.HandleCommandAsync;
                await Engine.Bot.DiscordCommandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: Engine.Bot.DiscordServiceProvider);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Registers global slash commands in Discord with their descriptions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// - Deletes all existing global commands
        /// - Registers:
        ///   - ping: Basic status check
        ///   - status: Bot status for admins
        ///   - weather: Weather lookup with location options
        /// - Logs command registration progress
        /// </remarks>
        [ConsoleSector("butterBror.Utils.DiscordWorker", "RegisterSlashCommands")]
        private static async Task RegisterSlashCommands()
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write("Discord - Updating commands...", "info");

            await Engine.Bot.Clients.Discord.Rest.DeleteAllGlobalCommandsAsync();

            await Engine.Bot.Clients.Discord.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Check bot status")
                .Build());
            await Engine.Bot.Clients.Discord.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("status")
                .WithDescription("View the bot's status. (Bot administrators only)")
                .Build());
            await Engine.Bot.Clients.Discord.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("weather")
                .WithDescription("Check the weather")
                .AddOption("location", ApplicationCommandOptionType.String, "weather check location", isRequired: false)
                .AddOption("showpage", ApplicationCommandOptionType.Integer, "show weather on page", isRequired: false)
                .AddOption("page", ApplicationCommandOptionType.Integer, "show the result page of the received weather", isRequired: false)
                .Build());

            Write("Discord - Commands updated!", "info");
        }
    }
}
