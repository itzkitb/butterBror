using butterBror.Utils.Tools;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using TwitchLib.Client.Events;
using static butterBror.Utils.Bot.Console;

namespace butterBror.Utils
{
    /// <summary>
    /// Contains event handlers for Discord API interactions and bot behavior customization.
    /// </summary>
    public partial class DiscordEvents
    {
        /// <summary>
        /// Handles Discord client logging events.
        /// </summary>
        /// <param name="log">The log message from Discord client.</param>
        /// <returns>A completed task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "LogAsync")]
        public static Task LogAsync(LogMessage log)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Write(ex);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Handles guild connection event when the bot joins a Discord server.
        /// </summary>
        /// <param name="g">The guild (server) that was connected to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ConnectToGuilt")]
        public static async Task ConnectToGuilt(SocketGuild g)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Discord - Connected to a server: {g.Name}", "info");
            Core.Bot.DiscordServers++;
        }

        /// <summary>
        /// Processes text-based commands from Discord users.
        /// </summary>
        /// <param name="arg">The message that might contain a command.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "HandleCommandAsync")]
        public static async Task HandleCommandAsync(SocketMessage arg)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                var message = arg as SocketUserMessage;
                if (message == null || message.Author.IsBot) return;

                int argPos = 0;
                if (message.HasCharPrefix(Core.Bot.Executor, ref argPos))
                {
                    var context = new SocketCommandContext(Core.Bot.Clients.Discord, message);
                    var result = await Core.Bot.DiscordCommandService.ExecuteAsync(context, argPos, Core.Bot.DiscordServiceProvider);
                    if (!result.IsSuccess)
                    {
                        Write($"Discord - {result.ErrorReason}", "info", LogLevel.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Handles slash command interactions from Discord users.
        /// </summary>
        /// <param name="command">The slash command interaction data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "SlashCommandHandler")]
        public static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Core.Statistics.FunctionsUsed.Add();
            Commands.Discord(command);
        }

        /// <summary>
        /// Handles application command creation events.
        /// </summary>
        /// <param name="e">The created application command data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ApplicationCommandCreated")]
        public static async Task ApplicationCommandCreated(SocketApplicationCommand e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - The command has been created: /" + e.Name + " (" + e.Description + ")", "info");
        }

        /// <summary>
        /// Handles application command deletion events.
        /// </summary>
        /// <param name="e">The deleted application command data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ApplicationCommandDeleted")]
        public static async Task ApplicationCommandDeleted(SocketApplicationCommand e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - Command deleted: /" + e.Name + " (" + e.Description + ")", "info");
        }

        /// <summary>
        /// Handles application command update events.
        /// </summary>
        /// <param name="e">The updated application command data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ApplicationCommandUpdated")]
        public static async Task ApplicationCommandUpdated(SocketApplicationCommand e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Discord - Command updated: /{e.Name} ({e.Description})", "info");
        }

        /// <summary>
        /// Handles channel creation events in Discord servers.
        /// </summary>
        /// <param name="e">The created channel data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ChannelCreated")]
        public static async Task ChannelCreated(SocketChannel e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - New channel created: " + e.Id, "info");
        }

        /// <summary>
        /// Handles channel deletion events in Discord servers.
        /// </summary>
        /// <param name="e">The deleted channel data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ChannelDeleted")]
        public static async Task ChannelDeleted(SocketChannel e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - The channel has been deleted: " + e.Id, "info");
        }

        /// <summary>
        /// Handles channel update events in Discord servers.
        /// </summary>
        /// <param name="e">The original channel data before update.</param>
        /// <param name="a">The updated channel data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ChannelUpdated")]
        public static async Task ChannelUpdated(SocketChannel e, SocketChannel a)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - Channel updated: " + e.Id + "/" + a.Id, "info");
        }

        /// <summary>
        /// Handles connection established event for the Discord client.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "Connected")]
        public static async Task Connected()
        {
            Core.Statistics.FunctionsUsed.Add();
            //Write("Discord - Connected!", "info");
        }

        /// <summary>
        /// Handles button interaction events from Discord message components.
        /// </summary>
        /// <param name="e">The button interaction data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [ConsoleSector("butterBror.Utils.DiscordEvents", "ButtonTouched")]
        public static async Task ButtonTouched(SocketMessageComponent e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Discord - A button was pressed. User: {e.User}, Button ID: {e.Id}, Server: {((SocketGuildChannel)e.Channel).Guild.Name}", "info");
        }
    }
}
