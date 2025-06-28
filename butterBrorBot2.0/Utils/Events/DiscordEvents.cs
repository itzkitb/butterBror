using butterBror.Utils.Tools;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using TwitchLib.Client.Events;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils
{
    public partial class DiscordEvents
    {
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

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ConnectToGuilt")]
        public static async Task ConnectToGuilt(SocketGuild g)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Discord - Connected to a server: {g.Name}", "info");
            Core.Bot.DiscordServers++;
        }

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

        [ConsoleSector("butterBror.Utils.DiscordEvents", "SlashCommandHandler")]
        public static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Core.Statistics.FunctionsUsed.Add();
            Commands.Discord(command);
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ApplicationCommandCreated")]
        public static async Task ApplicationCommandCreated(SocketApplicationCommand e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - The command has been created: /" + e.Name + " (" + e.Description + ")", "info");
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ApplicationCommandDeleted")]
        public static async Task ApplicationCommandDeleted(SocketApplicationCommand e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - Command deleted: /" + e.Name + " (" + e.Description + ")", "info");
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ApplicationCommandUpdated")]
        public static async Task ApplicationCommandUpdated(SocketApplicationCommand e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Discord - Command updated: /{e.Name} ({e.Description})", "info");
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ChannelCreated")]
        public static async Task ChannelCreated(SocketChannel e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - New channel created: " + e.Id, "info");
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ChannelDeleted")]
        public static async Task ChannelDeleted(SocketChannel e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - The channel has been deleted: " + e.Id, "info");
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ChannelUpdated")]
        public static async Task ChannelUpdated(SocketChannel e, SocketChannel a)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Discord - Channel updated: " + e.Id + "/" + a.Id, "info");
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "Connected")]
        public static async Task Connected()
        {
            Core.Statistics.FunctionsUsed.Add();
            //Write("Discord - Connected!", "info");
        }

        [ConsoleSector("butterBror.Utils.DiscordEvents", "ButtonTouched")]
        public static async Task ButtonTouched(SocketMessageComponent e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Discord - A button was pressed. User: {e.User}, Button ID: {e.Id}, Server: {((SocketGuildChannel)e.Channel).Guild.Name}", "info");
        }
    }

    public partial class DiscordWorker
    {
        [ConsoleSector("butterBror.Utils.DiscordWorker", "ReadyAsync")]
        public static async Task ReadyAsync()
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Discord - Connected as {Core.Bot.Clients.Discord.CurrentUser}!", "info");
                Core.Bot.DiscordServers = (ulong)Core.Bot.Clients.Discord.Guilds.Count;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.DiscordWorker", "MessageReceivedAsync")]
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

        [ConsoleSector("butterBror.Utils.DiscordWorker", "RegisterCommandsAsync")]
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

        [ConsoleSector("butterBror.Utils.DiscordWorker", "RegisterSlashCommands")]
        private static async Task RegisterSlashCommands()
        {
            Core.Statistics.FunctionsUsed.Add();
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

            Write("Discord - Commands updated!", "info");
        }
    }
}
