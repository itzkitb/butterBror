using butterBror.Utils.DataManagers;
using butterBror.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using Telegram.Bot.Types;
using static butterBror.Utils.Things.Console;
using Telegram.Bot;

namespace butterBror.Utils.Tools
{
    public class Chat
    {
        [ConsoleSector("butterBror.Utils.Tools.Chat", "ReturnFromAFK")]
        public static void ReturnFromAFK(string UserID, string RoomID, string channel, string username, string message_id, Message message_reply, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            var language = "ru";

            try
            {
                if (UsersData.Get<string>(UserID, "language", platform) == default)
                    UsersData.Save(UserID, "language", "ru", platform);
                else
                    language = UsersData.Get<string>(UserID, "language", platform);
            }
            catch (Exception ex)
            {
                Write(ex);
            }

            string? message = UsersData.Get<string>(UserID, "afkText", platform);
            if (!new NoBanwords().Check(message, RoomID, platform))
                return;

            string send = (Text.CleanAsciiWithoutSpaces(message) == "" ? "" : ": " + message);

            TimeSpan timeElapsed = DateTime.UtcNow - UsersData.Get<DateTime>(UserID, "afkTime", platform);
            var afkType = UsersData.Get<string>(UserID, "afkType", platform);
            string translateKey = "";

            if (afkType == "draw")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 8) translateKey = "draw:2h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "draw:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "draw:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "draw:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "draw:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "draw:1y";
                else translateKey = "draw:default";
            }
            else if (afkType == "afk")
            {
                if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 14) translateKey = "default:8h";
                else if (timeElapsed.TotalHours >= 14 && timeElapsed.TotalDays < 1) translateKey = "default:14h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "default:1d";
                else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "default:3d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 9) translateKey = "default:7d";
                else if (timeElapsed.TotalDays >= 9 && timeElapsed.TotalDays < 31) translateKey = "default:9d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "default:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "default:1y";
                else translateKey = "default";
            }
            else if (afkType == "sleep")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "sleep:2h";
                else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "sleep:5h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 12) translateKey = "sleep:8h";
                else if (timeElapsed.TotalHours >= 12 && timeElapsed.TotalDays < 1) translateKey = "sleep:12h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "sleep:1d";
                else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "sleep:3d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "sleep:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "sleep:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "sleep:1y";
                else translateKey = "sleep:default";
            }
            else if (afkType == "rest")
            {
                if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "rest:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "rest:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "rest:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "rest:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "rest:1y";
                else translateKey = "rest:default";
            }
            else if (afkType == "lurk") translateKey = "lurk:default";
            else if (afkType == "study")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "study:2h";
                else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "study:5h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "study:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "study:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "study:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "study:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "study:1y";
                else translateKey = "study:default";
            }
            else if (afkType == "poop")
            {
                if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalHours < 1) translateKey = "poop:1m";
                else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "poop:1h";
                else if (timeElapsed.TotalHours >= 8) translateKey = "poop:8h";
                else translateKey = "poop:default";
            }
            else if (afkType == "shower")
            {
                if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalMinutes < 10) translateKey = "shower:1m";
                else if (timeElapsed.TotalMinutes >= 10 && timeElapsed.TotalHours < 1) translateKey = "shower:10m";
                else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "shower:1h";
                else if (timeElapsed.TotalHours >= 8) translateKey = "shower:8h";
                else translateKey = "shower:default";
            }
            string text = TranslationManager.GetTranslation(language, "afk:" + translateKey, RoomID, platform); // FIX AA0
            UsersData.Save(UserID, "lastFromAfkResume", DateTime.UtcNow, platform);
            UsersData.Save(UserID, "isAfk", false, platform);

            if (platform.Equals(Platforms.Twitch))
                TwitchReply(channel, RoomID, text.Replace("%user%", username) + send + " (" + Text.FormatTimeSpan(timeElapsed, language) + ")", message_id, language, true);
            if (platform.Equals(Platforms.Telegram))
                TelegramReply(channel, message_reply.Chat.Id, text.Replace("%user%", username) + send + " (" + Text.FormatTimeSpan(timeElapsed, language) + ")", message_reply, language);
        }

        [ConsoleSector("butterBror.Utils.Tools.Chat", "TwitchSend")]
        public static void TwitchSend(string channel, string message, string channelID, string messageID, string lang, bool isSafeEx = false, bool asciiClean = true)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                //Write($"Twitch - A message was sent to the {channel} channel: {message}", "info");
                if (asciiClean)
                {
                    message = Text.CleanAscii(message);
                }

                if (message.Length > 1500)
                    message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID, Platforms.Twitch);
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);
                    string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                    message = string.Concat(message.AsSpan(0, splitIndex), "...");

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        TwitchSend(channel, channelID, part2, messageID, lang, isSafeEx);
                    });
                }

                if (!Core.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                    Core.Bot.Clients.Twitch.JoinChannel(channel);

                if (isSafeEx || new NoBanwords().Check(message, channelID, Platforms.Twitch))
                    Core.Bot.Clients.Twitch.SendMessage(channel, message);
                else
                    Core.Bot.Clients.Twitch.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID, Platforms.Twitch));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.Chat", "TwitchReply")]
        public static void TwitchReply(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx = false)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                //Write($"Twitch - A response to a message was sent to the {channel} channel: {message}", "info");
                message = Text.CleanAscii(message);

                if (message.Length > 1500)
                    message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID, Platforms.Twitch);
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);
                    string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                    message = string.Concat(message.AsSpan(0, splitIndex), "...");

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        TwitchReply(channel, channelID, part2, messageID, lang, isSafeEx);
                    });
                }

                if (!Core.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                    Core.Bot.Clients.Twitch.JoinChannel(channel);

                if (isSafeEx || new NoBanwords().Check(message, channelID, Platforms.Twitch))
                    Core.Bot.Clients.Twitch.SendReply(channel, messageID, message);
                else
                    Core.Bot.Clients.Twitch.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID, Platforms.Twitch));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.Chat", "TelegramReply")]
        public static void TelegramReply(string channel, long channelID, string message, Message messageReply, string lang, bool isSafeEx = false)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Telegram - A message was sent to {channel}: {message}", "info");

                if (message.Length > 4096)
                    message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID.ToString(), Platforms.Telegram);

                if (isSafeEx || new NoBanwords().Check(message, channelID.ToString(), Platforms.Telegram))
                    Core.Bot.Clients.Telegram.SendMessage(
                        channelID,
                        message,
                        replyParameters: messageReply.Id
                    );
                else
                    Core.Bot.Clients.Telegram.SendMessage(
                        channelID,
                        TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID.ToString(), Platforms.Telegram),
                        replyParameters: messageReply.Id
                    );
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.Chat", "SendReply")]
        public static void SendReply(Platforms platform, string channel, string channelID, string message, string language, string username, string userID, string server, string serverID, string messageID, Message messageReply, bool isSafe = false, ChatColorPresets usernameColor = ChatColorPresets.YellowGreen)
        {
            switch (platform)
            {
                case Platforms.Twitch:
                    Commands.SendCommandReply(new TwitchMessageSendData
                    {
                        message = message,
                        channel = channel,
                        channel_id = channelID,
                        message_id = messageID,
                        language = language,
                        username = username,
                        safe_execute = isSafe,
                        nickname_color = usernameColor
                    });
                    break;
                case Platforms.Discord:
                    Commands.SendCommandReply(new DiscordCommandSendData
                    {
                        message = message,
                        title = "",
                        description = "",
                        embed_color = Discord.Color.Green,
                        is_embed = false,
                        is_ephemeral = false,
                        server = server,
                        server_id = serverID,
                        language = language,
                        safe_execute = isSafe,
                        socket_command_base = null,
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        channel_id = channelID,
                        user_id = userID
                    });
                    break;
                case Platforms.Telegram:
                    Commands.SendCommandReply(new TelegramMessageSendData
                    {
                        message = message,
                        language = language,
                        safe_execute = isSafe,
                        channel = channel,
                        channel_id = channelID,
                        message_id = messageID,
                        username = username
                    });
                    break;
            }
        }
    }
}
