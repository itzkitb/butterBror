using butterBror.Utils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using TwitchLib.Client.Events;

namespace butterBror.BotUtils
{
    public partial class discord_events
    {
        public static Task LogAsync(LogMessage log)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Utils.Console.WriteLine(log.ToString().Replace("\n", " ").Replace("\r", ""), "discord");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"DiscordEventHandler\\LogAsync#{log.Message}");
                return Task.CompletedTask;
            }
        }
        public static async Task ConnectToGuilt(SocketGuild g)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[DS] Connected to a server: {g.Name}", "discord");
            Maintenance.connected_servers++;
        }
        public static async Task HandleCommandAsync(SocketMessage arg)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                var message = arg as SocketUserMessage;
                if (message == null || message.Author.IsBot) return;

                int argPos = 0;
                if (message.HasCharPrefix(Maintenance.executor, ref argPos))
                {
                    var context = new SocketCommandContext(Maintenance.discord_client, message);
                    var result = await Maintenance.discord_command_service.ExecuteAsync(context, argPos, Maintenance.discord_service_provider);
                    if (!result.IsSuccess)
                    {
                        Utils.Console.WriteLine(result.ErrorReason, "discord", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"DiscordEventHandler\\HandleCommandAsync");
            }
        }
        public static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Engine.Statistics.functions_used.Add();
            Commands.Discord(command);
        }
        public static async Task ApplicationCommandCreated(SocketApplicationCommand e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] The command has been created: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ApplicationCommandDeleted(SocketApplicationCommand e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Command deleted: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ApplicationCommandUpdated(SocketApplicationCommand e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Command updated: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ChannelCreated(SocketChannel e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] New channel created: " + e.Id, "discord");
        }
        public static async Task ChannelDeleted(SocketChannel e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] The channel has been deleted: " + e.Id, "discord");
        }
        public static async Task ChannelUpdated(SocketChannel e, SocketChannel a)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Channel updated: " + e.Id + "/" + a.Id, "discord");
        }
        public static async Task Connected()
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Connected!", "discord");
        }
        public static async Task ButtonTouched(SocketMessageComponent e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[DS] A button was pressed. User: {e.User}, Button ID: {e.Id}, Server: {((SocketGuildChannel)e.Channel).Guild.Name}", "info");
        }
    }

    public partial class DiscordWorker
    {
        public static async Task ReadyAsync()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Utils.Console.WriteLine($"[DS] Connected as {Maintenance.discord_client.CurrentUser}!", "discord");
                foreach (var guild in Maintenance.discord_client.Guilds)
                {
                    Utils.Console.WriteLine($"[DS] Connected to server: {guild.Name}", "discord");
                    Maintenance.connected_servers++;
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "DiscordWorker\\ReadyAsync");
            }
        }
        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                if (!(message is SocketUserMessage msg) || message.Author.IsBot) return;
                OnMessageReceivedArgs e = default;
                await Command.ProcessMessageAsync(message.Author.Id.ToString(), ((SocketGuildChannel)message.Channel).Guild.Id.ToString(), message.Author.Username.ToLower(), message.Content, e, ((SocketGuildChannel)message.Channel).Guild.Name, Platforms.Discord, null, message.Channel.ToString());

                if (message.Content.StartsWith(Maintenance.executor))
                {
                    Commands.Discord(message);
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"DiscordWorker\\MessageReceivedAsync#{message.Content}");
            }
        }
        public static async Task RegisterCommandsAsync()
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Maintenance.discord_client.Ready += RegisterSlashCommands;
                Maintenance.discord_client.MessageReceived += discord_events.HandleCommandAsync;
                await Maintenance.discord_command_service.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: Maintenance.discord_service_provider);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "DiscordWorker\\RegisterCommandsAsync");
            }
        }
        private static async Task RegisterSlashCommands()
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("[DS] Updating commands...", "discord");
            await Maintenance.discord_client.Rest.DeleteAllGlobalCommandsAsync();

            await Maintenance.discord_client.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Check bot status")
                .Build());
            await Maintenance.discord_client.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("status")
                .WithDescription("View the bot's status. (Bot administrators only)")
                .Build());
            await Maintenance.discord_client.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("weather")
                .WithDescription("Check the weather")
                .AddOption("location", ApplicationCommandOptionType.String, "weather check location", isRequired: false)
                .AddOption("showpage", ApplicationCommandOptionType.Integer, "show weather on page", isRequired: false)
                .AddOption("page", ApplicationCommandOptionType.Integer, "show the result page of the received weather", isRequired: false)
                .Build());
            Utils.Console.WriteLine("[DS] All commands are registered!", "discord");
        }
    }
}
