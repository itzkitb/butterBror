using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using Discord.WebSocket;
using Telegram.Bot;
using Telegram.Bot.Types;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace butterBror
{
    public class UserData
    {
        public required string id { get; set; }
        public required string language { get; set; }
        public required string username { get; set; }
        public int? balance { get; set; }
        public int? balance_floating { get; set; }
        public int? total_messages { get; set; }
        public bool? banned { get; set; }
        public bool? ignored { get; set; }
        public bool? channel_moderator { get; set; }
        public bool? channel_broadcaster { get; set; }
        public bool? bot_moderator { get; set; }
        public bool? bot_developer { get; set; }
    }
    public class CommandData
    {
        public required string name { set; get; }
        public List<string>? arguments { get; set; }
        public OnChatCommandReceivedArgs? twitch_arguments { get; set; }
        public Dictionary<string, dynamic>? discord_arguments { get; set; }
        public string message_id { get; set; }
        public required string user_id { get; set; }
        public string? channel { get; set; }
        public string? channel_id { get; set; }
        public required string arguments_string { get; set; }
        public SocketCommandBase? discord_command_base { get; set; }
        public required Platforms platform { get; set; }
        public required UserData user { get; set; }
        public required string command_instance_id { get; set; }
        public Message? telegram_message { get; set; }
    }
    public enum Platforms
    {
        Twitch,
        Discord,
        Telegram
    }

    public class Platform
    {
        public static Dictionary<int, string> strings = new()
        {
            { 0, "TWITCH" },
            { 1, "DISCORD"},
            { 2, "TELEGRAM"}
        };
    }

    public class TwitchMessageSendData
    {
        public required string message { get; set; }
        public required string channel { get; set; }
        public required string channel_id { get; set; }
        public required string message_id { get; set; }
        public required string language { get; set; }
        public required string username { get; set; }
        public required bool safe_execute { get; set; }
        public required ChatColorPresets nickname_color { get; set; }
    }
    public class TelegramMessageSendData
    {
        public required string message_id { get; set; }
        public required string message { get; set; }
        public required string channel { get; set; }
        public required string channel_id { get; set; }
        public required string language { get; set; }
        public required string username { get; set; }
        public required bool safe_execute { get; set; }
    }
    public class DiscordCommandSendData
    {
        public required string description { get; set; }
        public string? author { get; set; }
        public string? image_link { get; set; }
        public string? thumbnail_link { get; set; }
        public string? footer { get; set; }
        public required bool is_embed { get; set; }
        public required bool is_ephemeral { get; set; }
        public string? title { get; set; }
        public Discord.Color? embed_color { get; set; }
        public required string server { get; set; }
        public required string server_id { get; set; }
        public required string language { get; set; }
        public string? message { get; set; }
        public required bool safe_execute { get; set; }
        public required SocketCommandBase socket_command_base { get; set; }
    }
    public class CommandInfo
    {
        public required string name { get; set; }
        public required string author { get; set; }
        public required string author_link { get; set; }
        public required string author_avatar { get; set; }
        public required string[] aliases { get; set; }
        public required int cooldown_global { get; set; }
        public required int cooldown_per_user { get; set; }
        public required Dictionary<string, string> description { get; set; }
        public required string wiki_link { get; set; }
        public required string arguments { get; set; }
        public required bool cooldown_reset { get; set; }
        public required DateTime creation_date { get; set; }
        public required bool is_for_bot_moderator { get; set; }
        public required bool is_for_channel_moderator { get; set; }
        public required bool is_for_bot_developer { get; set; }
        public double? cost { get; set; }
        public required Platforms[] platforms { get; set; }

        public bool is_on_development { get; set; }
    }
    public class CommandReturn
    {
        public required string message { get; set; }
        public required bool safe_execute { get; set; }
        public required string description { get; set; }
        public required string author { get; set; }
        public required string image_link { get; set; }
        public required string thumbnail_link { get; set; }
        public required string footer { get; set; }
        public required bool is_embed { get; set; }
        public required bool is_ephemeral { get; set; }
        public required string title { get; set; }
        public required System.Drawing.Color embed_color { get; set; }
        public required ChatColorPresets nickname_color { get; set; }
        public bool is_error = false;
        public Exception? exception { get; set; }
    }
    public partial class Commands
    {
        public static async void SendCommandReply(TwitchMessageSendData data)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                string message = data.message;
                TwitchMessageSendData messageToSendPart2 = null;
                Utils.Console.WriteLine("[TW] Sending a message...", "info");
                LogWorker.Log($"[TW] A response to message {data.message_id} was sent to channel {data.channel}: {data.message}", LogWorker.LogTypes.Msg, "ButterBib\\Commands\\SendCommandReply");
                message = TextUtil.CleanAscii(data.message);

                if (message.Length > 1500)
                    message = TranslationManager.GetTranslation(data.language, "error:too_large_text", data.channel_id, Platforms.Twitch);
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);
                    string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                    message = string.Concat(message.AsSpan(0, splitIndex), "...");

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        Utils.Chat.TwitchReply(data.channel, data.channel_id, part2, data.message_id, data.language, data.safe_execute);
                    });
                }

                if (!Maintenance.twitch_client.JoinedChannels.Contains(new JoinedChannel(data.channel)))
                    Maintenance.twitch_client.JoinChannel(data.channel);

                if (data.safe_execute || NoBanwords.Check(message, data.channel_id, Platforms.Twitch))
                    Maintenance.twitch_client.SendReply(data.channel, data.message_id, message);
                else
                    Maintenance.twitch_client.SendReply(data.channel, data.message_id, TranslationManager.GetTranslation(data.language, "error:message_could_not_be_sent", data.channel_id, Platforms.Twitch));
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"butterBib\\Commands\\SendCommandReply");
            }
        }
        public static async void SendCommandReply(TelegramMessageSendData data)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                string messageToSend = data.message;
                TelegramMessageSendData messageToSendPart2 = null;
                Utils.Console.WriteLine($"[TG] Sending a message... (Room: {(data.channel_id == null ? "null" : data.channel_id)}, message ID: {data.message_id})", "info");
                LogWorker.Log($"[TG] A message response was sent to the {data.channel} channel: {data.message}", LogWorker.LogTypes.Msg, "ButterBib\\Commands\\SendCommandReply");
                messageToSend = TextUtil.CleanAscii(data.message);

                if (messageToSend.Length > 1500)
                {
                    messageToSend = TranslationManager.GetTranslation(data.language, "error:too_large_text", data.channel_id, Platforms.Telegram);
                }
                else if (messageToSend.Length > 500)
                {
                    int splitIndex = messageToSend.LastIndexOf(' ', 450);

                    string part1 = messageToSend.Substring(0, splitIndex) + "...";
                    string part2 = "..." + messageToSend.Substring(splitIndex);

                    messageToSend = part1;
                    messageToSendPart2 = data;
                    messageToSendPart2.message = part2;
                }

                if (!Maintenance.twitch_client.JoinedChannels.Any(c => c.Channel == data.channel))
                    Maintenance.twitch_client.JoinChannel(data.channel);
                if (Maintenance.twitch_client.JoinedChannels.Any(c => c.Channel == data.channel))
                {
                    if (data.safe_execute || NoBanwords.Check(messageToSend, data.channel_id, Platforms.Telegram))
                        await Maintenance.telegram_client.SendMessage(long.Parse(data.channel_id), data.message, replyParameters: int.Parse(data.message_id));
                    else
                        await Maintenance.telegram_client.SendMessage(long.Parse(data.channel_id), TranslationManager.GetTranslation(data.language, "error:message_could_not_be_sent", data.channel_id, Platforms.Telegram), replyParameters: int.Parse(data.message_id));
                }

                if (messageToSendPart2 != null)
                {
                    await Task.Delay(1500);
                    SendCommandReply(messageToSendPart2);
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"butterBib\\Commands\\SendCommandReply");
            }
        }
        public static async void SendCommandReply(DiscordCommandSendData data)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                Utils.Console.WriteLine("[DS] Sending a message...", "info");
                LogWorker.Log($"[DS] A response to the command was sent to the server {data.server}: {data.message}", LogWorker.LogTypes.Msg, "ButterBib\\Commands\\SendDiscordReply");
                data.message = TextUtil.CleanAscii(data.message);

                if (data.message.Length > 1500)
                {
                    data.message = TranslationManager.GetTranslation(data.language, "error:too_large_text", "", Platforms.Discord);
                }
                else if (data.message.Length > 500)
                {
                    int splitIndex = data.message.LastIndexOf(' ', 450);

                    string part1 = data.message.Substring(0, splitIndex) + "...";
                    string part2 = "... " + data.message.Substring(splitIndex);

                    data.message = part1;

                    await Task.Delay(1000);
                    SendCommandReply(data);
                }

                if (data.safe_execute || data.is_ephemeral)
                {
                    if (data.is_embed)
                    {
                        var embed = new EmbedBuilder();
                        if (data.title != "")
                            embed.WithTitle(data.title);
                        if (data.embed_color != default(Discord.Color))
                            embed.WithColor((Discord.Color)data.embed_color);
                        if (data.description != "")
                            embed.WithDescription(data.description);
                        if (data.thumbnail_link != "")
                            embed.WithThumbnailUrl(data.thumbnail_link);
                        if (data.image_link != "")
                            embed.WithImageUrl(data.image_link);

                        var resultEmbed = embed.Build();
                        data.socket_command_base.RespondAsync(embed: resultEmbed, ephemeral: data.is_ephemeral);
                    }
                    else
                    {
                        data.socket_command_base.RespondAsync(data.message, ephemeral: data.is_ephemeral);
                    }
                }
                else if (NoBanwords.Check(data.message, data.server_id, Platforms.Discord) && NoBanwords.Check(data.description, data.server_id, Platforms.Discord))
                {
                    if (data.is_embed)
                    {
                        var embed = new EmbedBuilder();
                        if (data.title != "")
                            embed.WithTitle(data.title);
                        if (data.embed_color != default(Discord.Color))
                            embed.WithColor((Discord.Color)data.embed_color);
                        if (data.description != "")
                            embed.WithDescription(data.description);
                        if (data.thumbnail_link != "")
                            embed.WithThumbnailUrl(data.thumbnail_link);
                        if (data.image_link != "")
                            embed.WithImageUrl(data.image_link);

                        var resultEmbed = embed.Build();
                        data.socket_command_base.RespondAsync(embed: resultEmbed, ephemeral: data.is_ephemeral);
                    }
                    else
                    {
                        data.socket_command_base.RespondAsync(data.message, ephemeral: data.is_ephemeral);
                    }
                }
                else
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(TranslationManager.GetTranslation(data.language, "error:message_could_not_be_sent", "", Platforms.Discord))
                        .WithColor(global::Discord.Color.Red)
                        .Build();
                    data.socket_command_base.RespondAsync(embed: embed, ephemeral: data.is_ephemeral);
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"butterBib\\Commands\\SendCommandReply");
            }
        }
    }
}
