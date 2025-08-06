using Discord.WebSocket;
using Discord;
using System.Reflection;
using TwitchLib.Client.Events;
using static butterBror.Core.Bot.Console;
using butterBror.Core.Commands;
using butterBror.Models;
using butterBror.Utils;

namespace butterBror.Workers
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
        
        public static async Task ReadyAsync()
        {
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
        
        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            try
            {
                if (!(message is SocketUserMessage msg) || message.Author.IsBot) return;
                
                await Command.ProcessMessageAsync(
                    message.Author.Id.ToString(),
                    message.Channel.Id.ToString(),
                    message.Author.Username.ToLower(),
                    message.Content,
                    null,
                    message.Channel.Name,
                    PlatformsEnum.Discord,
                    null,
                    message.Id.ToString(),
                    ((SocketGuildChannel)message.Channel).Guild.Name,
                    ((SocketGuildChannel)message.Channel).Guild.Id.ToString());

                if (message.Content.StartsWith(Engine.Bot.Executor))
                {
                    Executor.Discord(message);
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
        
        public static async Task RegisterCommandsAsync()
        {
            try
            {
                Engine.Bot.Clients.Discord.Ready += RegisterSlashCommands;
                Engine.Bot.Clients.Discord.MessageReceived += Events.DiscordEvents.HandleCommandAsync;
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
        
        private static async Task RegisterSlashCommands()
        {
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
