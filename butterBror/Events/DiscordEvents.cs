using butterBror.Core.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static butterBror.Core.Bot.Console;

namespace butterBror.Events
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

        public static Task LogAsync(LogMessage log)
        {
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

        public static async Task ConnectToGuilt(SocketGuild g)
        {
            Write($"Discord - Connected to a server: {g.Name}", "info");
            Bot.DiscordServers++;
        }

        /// <summary>
        /// Processes text-based commands from Discord users.
        /// </summary>
        /// <param name="arg">The message that might contain a command.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task HandleCommandAsync(SocketMessage arg)
        {
            try
            {
                var message = arg as SocketUserMessage;
                if (message == null || message.Author.IsBot) return;

                int argPos = 0;
                if (message.HasCharPrefix(Bot.DefaultExecutor, ref argPos))
                {
                    var context = new SocketCommandContext(Bot.Clients.Discord, message);
                    var result = await Bot.DiscordCommandService.ExecuteAsync(context, argPos, Bot.DiscordServiceProvider);
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

        public static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Executor.Discord(command);
        }

        /// <summary>
        /// Handles application command creation events.
        /// </summary>
        /// <param name="e">The created application command data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task ApplicationCommandCreated(SocketApplicationCommand e)
        {
            Write("Discord - The command has been created: /" + e.Name + " (" + e.Description + ")", "info");
        }

        /// <summary>
        /// Handles application command deletion events.
        /// </summary>
        /// <param name="e">The deleted application command data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task ApplicationCommandDeleted(SocketApplicationCommand e)
        {
            Write("Discord - Command deleted: /" + e.Name + " (" + e.Description + ")", "info");
        }

        /// <summary>
        /// Handles application command update events.
        /// </summary>
        /// <param name="e">The updated application command data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task ApplicationCommandUpdated(SocketApplicationCommand e)
        {
            Write($"Discord - Command updated: /{e.Name} ({e.Description})", "info");
        }

        /// <summary>
        /// Handles channel creation events in Discord servers.
        /// </summary>
        /// <param name="e">The created channel data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task ChannelCreated(SocketChannel e)
        {
            Write("Discord - New channel created: " + e.Id, "info");
        }

        /// <summary>
        /// Handles channel deletion events in Discord servers.
        /// </summary>
        /// <param name="e">The deleted channel data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task ChannelDeleted(SocketChannel e)
        {
            Write("Discord - The channel has been deleted: " + e.Id, "info");
        }

        /// <summary>
        /// Handles channel update events in Discord servers.
        /// </summary>
        /// <param name="e">The original channel data before update.</param>
        /// <param name="a">The updated channel data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task ChannelUpdated(SocketChannel e, SocketChannel a)
        {
            Write("Discord - Channel updated: " + e.Id + "/" + a.Id, "info");
        }

        /// <summary>
        /// Handles connection established event for the Discord client.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task Connected()
        {
            //Write("Discord - Connected!", "info");
        }

        /// <summary>
        /// Handles button interaction events from Discord message components.
        /// </summary>
        /// <param name="e">The button interaction data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>

        public static async Task ButtonTouched(SocketMessageComponent e)
        {
            Write($"Discord - A button was pressed. User: {e.User}, Button ID: {e.Id}, Server: {((SocketGuildChannel)e.Channel).Guild.Name}", "info");
        }
    }
}
