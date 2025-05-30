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
        public string server_id { get; set; }
        public string server { get; set; }
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
        public required string channel_id { get; set; }
        public required string language { get; set; }
        public string? message { get; set; }
        public required bool safe_execute { get; set; }
        public required SocketCommandBase socket_command_base { get; set; }
        public required string user_id { get; set; }
    }
    public class CommandInfo
    {
        public required string Name { get; set; }
        public required string Author { get; set; }
        public required string AuthorLink { get; set; }
        public required string AuthorAvatar { get; set; }
        public required string[] Aliases { get; set; }
        public required int CooldownPerChannel { get; set; }
        public required int CooldownPerUser { get; set; }
        public required Dictionary<string, string> Description { get; set; }
        public required string WikiLink { get; set; }
        public required string Arguments { get; set; }
        public required bool CooldownReset { get; set; }
        public required DateTime CreationDate { get; set; }
        public required bool IsForBotModerator { get; set; }
        public required bool IsForChannelModerator { get; set; }
        public required bool IsForBotDeveloper { get; set; }
        public double? cost { get; set; }
        public required Platforms[] Platforms { get; set; }

        public bool is_on_development { get; set; }
    }
    public class CommandReturn
    {
        private System.Drawing.Color TwitchColors(ChatColorPresets color)
        {
            switch (color)
            {
                case ChatColorPresets.Blue:
                    return System.Drawing.Color.FromArgb(255, 0, 52, 252);
                case ChatColorPresets.BlueViolet:
                    return System.Drawing.Color.FromArgb(255, 146, 0, 255);
                case ChatColorPresets.CadetBlue:
                    return System.Drawing.Color.FromArgb(255, 85, 140, 135);
                case ChatColorPresets.Chocolate:
                    return System.Drawing.Color.FromArgb(255, 255, 127, 36);
                case ChatColorPresets.Coral:
                    return System.Drawing.Color.FromArgb(255, 255, 127, 80);
                case ChatColorPresets.DodgerBlue:
                    return System.Drawing.Color.FromArgb(255, 30, 144, 255);
                case ChatColorPresets.Firebrick:
                    return System.Drawing.Color.FromArgb(255, 178, 34, 34);
                case ChatColorPresets.GoldenRod:
                    return System.Drawing.Color.FromArgb(255, 218, 165, 32);
                case ChatColorPresets.Green:
                    return System.Drawing.Color.FromArgb(255, 0, 128, 0);
                case ChatColorPresets.HotPink:
                    return System.Drawing.Color.FromArgb(255, 255, 105, 180);
                case ChatColorPresets.OrangeRed:
                    return System.Drawing.Color.FromArgb(255, 255, 69, 0);
                case ChatColorPresets.Red:
                    return System.Drawing.Color.FromArgb(255, 255, 0, 0);
                case ChatColorPresets.SeaGreen:
                    return System.Drawing.Color.FromArgb(255, 46, 139, 87);
                case ChatColorPresets.SpringGreen:
                    return System.Drawing.Color.FromArgb(255, 0, 255, 127);
                case ChatColorPresets.YellowGreen:
                    return System.Drawing.Color.FromArgb(255, 154, 205, 50);
                default:
                    return System.Drawing.Color.FromArgb(255, 0, 0, 0);
            }
        }

        public CommandReturn()
        {
            this.Message = "PauseChamp Empty result. Report that to @ItzKITb";
            this.IsSafe = false;
            this.Description = string.Empty;
            this.Author = string.Empty;
            this.ImageLink = string.Empty;
            this.ThumbnailLink = string.Empty;
            this.Footer = string.Empty;
            this.IsEmbed = false;
            this.IsEphemeral = false;
            this.Title = string.Empty;
            this.EmbedColor = System.Drawing.Color.Green;
            this.BotNameColor = ChatColorPresets.YellowGreen;
            this.IsError = false;
            this.Exception = null;
        }

        public void SetMessage(string Message)
        {
            this.Message = Message;
        }

        public void SetMessage(string Message, bool IsSafe)
        {
            this.Message = Message;
            this.IsSafe = IsSafe;
        }

        public void SetMessage(string Message, string Title, bool IsSafe)
        {
            this.Message = Message;
            this.IsSafe = IsSafe;
            this.Title = Title;
        }

        public void SetError(Exception ex)
        {
            this.Exception = ex;
            this.IsError = true;
        }

        public void SetEmbed(string ImageLink = "", string ThumbnailLink = "", string Footer = "", string Title = "", string Description = "", string Author = "")
        {
            if (ImageLink != "") this.ImageLink = ImageLink;
            if (ThumbnailLink != "") this.ThumbnailLink = ThumbnailLink;
            if (Footer != "") this.Footer = Footer;
            if (Title != "") this.Title = Title;
            if (Description != "") this.Description = Description;
            if (Author != "") this.Author = Author;
        }

        public void SetColor(ChatColorPresets NicknameColor)
        {
            this.BotNameColor = NicknameColor;
            this.EmbedColor = TwitchColors(NicknameColor);
        }

        public void SetEmbed(bool IsEmbed)
        {
            this.IsEmbed = IsEmbed;
        }

        public void SetEphemeral(bool IsEphemeral)
        {
            this.IsEphemeral = IsEphemeral;
        }

        public void SetSafe(bool IsSafe)
        {
            this.IsSafe = IsSafe;
        }

        public string Message { get; private set; }
        public bool IsSafe { get; private set; }
        public string Description { get; private set; }
        public string Author { get; private set; }
        public string ImageLink { get; private set; }
        public string ThumbnailLink { get; private set; }
        public string Footer { get; private set; }
        public bool IsEmbed { get; private set; }
        public bool IsEphemeral { get; private set; }
        public string Title { get; private set; }
        public System.Drawing.Color EmbedColor { get; private set; }
        public ChatColorPresets BotNameColor { get; private set; }
        public bool IsError { get; private set; }
        public Exception? Exception { get; private set; }
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

                if (data.safe_execute || new NoBanwords().Check(message, data.channel_id, Platforms.Twitch))
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

                if (messageToSend.Length > 12288)
                {
                    messageToSend = TranslationManager.GetTranslation(data.language, "error:too_large_text", data.channel_id, Platforms.Telegram);
                }
                else if (messageToSend.Length > 4096)
                {
                    int splitIndex = messageToSend.LastIndexOf(' ', 4000);

                    string part1 = messageToSend.Substring(0, splitIndex) + "...";
                    string part2 = "..." + messageToSend.Substring(splitIndex);

                    messageToSend = part1;
                    messageToSendPart2 = data;
                    messageToSendPart2.message = part2;
                }

                if (data.safe_execute || new NoBanwords().Check(messageToSend, data.channel_id, Platforms.Telegram))
                    await Maintenance.telegram_client.SendMessage(long.Parse(data.channel_id), data.message, replyParameters: int.Parse(data.message_id));
                else
                    await Maintenance.telegram_client.SendMessage(long.Parse(data.channel_id), TranslationManager.GetTranslation(data.language, "error:message_could_not_be_sent", data.channel_id, Platforms.Telegram), replyParameters: int.Parse(data.message_id));
                

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

                if (data.socket_command_base != null)
                {
                    if ((data.safe_execute | data.is_ephemeral) || new NoBanwords().Check(data.message, data.server_id, Platforms.Discord) && new NoBanwords().Check(data.description, data.server_id, Platforms.Discord))
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
                else
                {
                    string messageToSend = "";
                    DiscordCommandSendData messageToSendPart2;
                    if (data.message.Length > 12288)
                    {
                        messageToSend = TranslationManager.GetTranslation(data.language, "error:too_large_text", data.channel_id, Platforms.Telegram);
                    }
                    else if (messageToSend.Length > 4096)
                    {
                        int splitIndex = messageToSend.LastIndexOf(' ', 4000);

                        string part1 = messageToSend.Substring(0, splitIndex) + "...";
                        string part2 = "..." + messageToSend.Substring(splitIndex);

                        messageToSend = part1;
                        messageToSendPart2 = data;
                        messageToSendPart2.message = part2;
                    }

                    ITextChannel sender = await Maintenance.discord_client.GetChannelAsync(ulong.Parse(data.channel_id)) as ITextChannel;

                    if ((data.safe_execute | data.is_ephemeral) || new NoBanwords().Check(data.message, data.server_id, Platforms.Discord) && new NoBanwords().Check(data.description, data.server_id, Platforms.Discord))
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
                            sender.SendMessageAsync($"<@{data.user_id}> {data.message}", embed: resultEmbed);
                        }
                        else
                        {
                            sender.SendMessageAsync($"<@{data.user_id}> {data.message}");
                        }
                    }
                    else
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle(TranslationManager.GetTranslation(data.language, "error:message_could_not_be_sent", "", Platforms.Discord))
                            .WithColor(global::Discord.Color.Red)
                            .Build();
                        sender.SendMessageAsync($"<@{data.user_id}> {data.message}", embed: embed);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"butterBib\\Commands\\SendCommandReply");
            }
        }
    }
}
