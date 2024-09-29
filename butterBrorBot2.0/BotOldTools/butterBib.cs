using butterBror;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using Discord.WebSocket;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;

namespace butterBib
{
    public class UserData
    {
        public required string Id { get; set; }
        public required string Lang { get; set; }
        public required string Name { get; set; }
        public int ?Balance { get; set; }
        public int ?floatBalance { get; set; }
        public int ?totalMessages { get; set; }
        public bool ?IsBanned { get; set; }
        public bool? IsIgnored { get; set; }
        public bool? IsChannelAdmin { get; set; }
        public bool? IsChannelBroadcaster { get; set; }
        public bool? IsBotAdmin { get; set; }
        public bool? IsBotCreator { get; set; }
    }
    public class CommandData
    {
        public required string Name {  set; get; }
        public List<string> ?args { get; set; }
        public OnChatCommandReceivedArgs ?TWargs { get; set; }
        public Dictionary<string, dynamic> ?DSargs { get; set; }
        public required string RequestUUID { get; set; }
        public required string UserUUID { get; set; }
        public string ?MessageID { get; set; }
        public string ?Channel {  get; set; }
        public string ?ChannelID { get; set; }
        public required string ArgsAsString { get; set; }
        public SocketCommandBase ?d { get; set; }
        public required Platforms Platform { get; set; }
        public required UserData User { get; set; }
    }
    public enum Platforms
    {
        Twitch,
        Discord
    }
    public class TwitchMessageSendData
    {
        public required string Message { get; set; }
        public required string Channel { get; set; }
        public required string ChannelID { get; set; }
        public required string AnswerID { get; set; }
        public required string Lang { get; set; }
        public required string Name { get; set; }
        public required bool IsSafeExecute { get; set; }
        public required ChatColorPresets NickNameColor { get; set; }
    }
    public class DiscordCommandSendData
    {
        public required string Description { get; set; }
        public string ?Author { get; set; }
        public string ?ImageURL { get; set; }
        public string ?ThumbnailUrl { get; set; }
        public string ?Footer { get; set; }
        public required bool IsEmbed { get; set; }
        public required bool Ephemeral { get; set; }
        public string ?Title { get; set; }
        public Color ?Color { get; set; }
        public required string Server { get; set; }
        public required string ServerID { get; set; }
        public required string Lang { get; set; }
        public string ?Message { get; set; }
        public required bool IsSafeExecute { get; set; }
        public required SocketCommandBase d { get; set; }
    }
    public class CommandInfo
    {
        public required string Name { get; set; }
        public required string Author { get; set; }
        public required string AuthorURL { get; set; }
        public required string AuthorImageURL { get; set; }
        public required string[] aliases { get; set; }
        public required int GlobalCooldown { get; set; }
        public required int UserCooldown { get; set; }
        public required string Description { get; set; }
        public required string UseURL { get; set; }
        public required string ArgsRequired { get; set; }
        public required bool ResetCooldownIfItHasNotReachedZero { get; set; }
        public required DateTime CreationDate { get; set; }
        public required bool ForAdmins { get; set; }
        public required bool ForChannelAdmins { get; set; }
        public required bool ForBotCreator { get; set; }
        public double? Cost { get; set; }
    }
    public class CommandReturn
    {
        public required string Message { get; set; }
        public required bool IsSafeExecute { get; set; }
        public required string Description { get; set; }
        public required string Author { get; set; }
        public required string ImageURL { get; set; }
        public required string ThumbnailUrl { get; set; }
        public required string Footer { get; set; }
        public required bool IsEmbed { get; set; }
        public required bool Ephemeral { get; set; }
        public required string Title { get; set; }
        public required Color Color { get; set; }
        public required ChatColorPresets NickNameColor { get; set; }
    }

    public class CommandAnswer
    {
        public required bool IsSucceful { get; set; }
        public required bool IsNeedToResponse { get; set; }
        public TwitchMessageSendData ?TwAnswer { get; set; }
        public DiscordCommandSendData ?DsAnswer { get; set; }
    }
    public partial class Commands
    {
        public static async void SendCommandReply(TwitchMessageSendData data)
        {
            ConsoleServer.SendConsoleMessage("commands", "Отправка сообщения...");
            LogWorker.Log($"Был отправлен ответ на сообщение в канал {data.Channel}: {data.Message}", LogWorker.LogTypes.Msg, "send_command_reply");
            data.Message = TextUtil.FilterText(data.Message);
            await CommandUtil.ChangeNicknameColorAsync(data.NickNameColor);

            if (data.Message.Length > 1500)
            {
                data.Message = TranslationManager.GetTranslation(data.Lang, "tooLargeText", data.ChannelID);
            }
            else if (data.Message.Length > 500)
            {
                int splitIndex = data.Message.LastIndexOf(' ', 450);

                string part1 = data.Message.Substring(0, splitIndex) + "...";
                string part2 = "..." + data.Message.Substring(splitIndex);

                data.Message = part2;

                await Task.Delay(1000);
                SendCommandReply(data);
            }

            if (!Bot.client.JoinedChannels.Any(c => c.Channel == data.Channel))
            {
                Bot.client.JoinChannel(data.Channel);
            }
            if (Bot.client.JoinedChannels.Any(c => c.Channel == data.Channel))
            {
                if (data.IsSafeExecute)
                {
                    Bot.client.SendReply(data.Channel, data.AnswerID, data.Message);
                }
                else if (NoBanwords.fullCheck(data.Message, data.ChannelID))
                {
                    Bot.client.SendReply(data.Channel, data.AnswerID, data.Message);
                }
                else
                {
                    Bot.client.SendReply(data.Channel, data.AnswerID, TranslationManager.GetTranslation(data.Lang, "cantSend", data.ChannelID));
                }
            }
        }
        public static async void SendCommandReply(DiscordCommandSendData data)
        {
            ConsoleServer.SendConsoleMessage("discord", "Отправка сообщения...");
            LogWorker.Log($"Был отправлен ответ на комманду, на сервер {data.Server}: {data.Message}", LogWorker.LogTypes.Msg, "send_command_reply_discord");
            data.Message = TextUtil.FilterText(data.Message);

            if (data.Message.Length > 1500)
            {
                data.Message = TranslationManager.GetTranslation(data.Lang, "tooLargeText", "");
            }
            else if (data.Message.Length > 500)
            {
                int splitIndex = data.Message.LastIndexOf(' ', 450);

                string part1 = data.Message.Substring(0, splitIndex) + "...";
                string part2 = "... " + data.Message.Substring(splitIndex);

                data.Message = part1;

                await Task.Delay(1000);
                SendCommandReply(data);
            }

            if (data.IsSafeExecute || data.Ephemeral)
            {
                if (data.IsEmbed)
                {
                    var embed = new EmbedBuilder();
                    if (data.Title != "")
                    {
                        embed.WithTitle(data.Title);
                    }
                    if (data.Color != default(Color))
                    {
                        embed.WithColor((Color)data.Color);
                    }
                    if (data.Description != "")
                    {
                        embed.WithDescription(data.Description);
                    }
                    if (data.ThumbnailUrl != "")
                    {
                        embed.WithThumbnailUrl(data.ThumbnailUrl);
                    }
                    if (data.ImageURL != "")
                    {
                        embed.WithImageUrl(data.ImageURL);
                    }
                    var resultEmbed = embed.Build();
                    data.d.RespondAsync(embed: resultEmbed, ephemeral: data.Ephemeral);
                }
                else
                {
                    data.d.RespondAsync(data.Message, ephemeral: data.Ephemeral);
                }
            }
            else if (NoBanwords.fullCheck(data.Message, data.ServerID) && NoBanwords.fullCheck(data.Description, data.ServerID))
            {
                if (data.IsEmbed)
                {
                    var embed = new EmbedBuilder();
                    if (data.Title != "")
                    {
                        embed.WithTitle(data.Title);
                    }
                    if (data.Color != default(Color))
                    {
                        embed.WithColor((Color)data.Color);
                    }
                    if (data.Description != "")
                    {
                        embed.WithDescription(data.Description);
                    }
                    if (data.ThumbnailUrl != "")
                    {
                        embed.WithThumbnailUrl(data.ThumbnailUrl);
                    }
                    if (data.ImageURL != "")
                    {
                        embed.WithImageUrl(data.ImageURL);
                    }
                    var resultEmbed = embed.Build();
                    data.d.RespondAsync(embed: resultEmbed, ephemeral: data.Ephemeral);
                }
                else
                {
                    data.d.RespondAsync(data.Message, ephemeral: data.Ephemeral);
                }
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithTitle(TranslationManager.GetTranslation(data.Lang, "cantSend", ""))
                    .WithColor(Color.Red)
                    .Build();
                data.d.RespondAsync(embed: embed, ephemeral: data.Ephemeral);
            }
        }
    }
}
