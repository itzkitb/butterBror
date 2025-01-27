using System.Net.NetworkInformation;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBib;

namespace butterBror
{
    public partial class Commands
    {
        public class Pinger
        {
            public static CommandInfo Info = new()
            {
                Name = "Ping",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Этой командой можно узнать, функционирует ли бот и получить краткую информацию о нем.",
                UseURL = "https://itzkitb.ru/bot/command?name=ping",
                UserCooldown = 30,
                GlobalCooldown = 5,
                aliases = ["ping", "пинг", "понг", "пенг", "п"],
                ArgsRequired = "(ISP/dev)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string arg1 = "";
                    if (data.args.Count > 0)
                        arg1 = data.args[0].ToLower();

                    string returnMessage = "";
                    if (data.args.Count == 0)
                    {
                        var workTime = DateTime.Now - BotEngine.botStartTime;
                        string host = "";
                        if (data.Platform == Platforms.Discord) host = "discord.com";
                        else if (data.Platform == Platforms.Twitch) host = "twitch.tv";
                        else if (data.Platform == Platforms.Telegram) host = "t.me";
                        PingReply reply = new Ping().Send(host, 1000);
                        long pingSpeed = -1;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;

                        returnMessage = TranslationManager.GetTranslation(data.User.Lang, "commandPingMain", data.ChannelID)
                                    .Replace("%version%", BotEngine.botVersion)
                                    .Replace("%workTime%", TextUtil.FormatTimeSpan(workTime, data.User.Lang))
                                    .Replace("%tabs%", Bot.Channels.Length.ToString())
                                    .Replace("%loadedCMDs%", Bot.CommandsActive.ToString())
                                    .Replace("%completedCMDs%", BotEngine.completedCommands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString());
                    }
                    else if (arg1.Equals("isp"))
                    {
                        var workTime = DateTime.Now - BotEngine.botStartTime;
                        PingReply reply = new Ping().Send("192.168.1.1", 1000);
                        long pingSpeed = -1;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        else
                        {
                            reply = new Ping().Send("192.168.0.1", 1000);
                            if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        }
                        
                        returnMessage = TranslationManager.GetTranslation(data.User.Lang, "commandPingIsp", data.ChannelID)
                                    .Replace("%ping%", pingSpeed.ToString());
                    }
                    else if (arg1.Equals("dev"))
                    {
                        var workTime = DateTime.Now - BotEngine.botStartTime;
                        string host = "";
                        if (data.Platform == Platforms.Discord) host = "discord.com";
                        else if (data.Platform == Platforms.Twitch) host = "twitch.tv";
                        else if (data.Platform == Platforms.Telegram) host = "t.me";

                        PingReply reply = new Ping().Send(host, 1000);
                        long pingSpeed = 0;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;

                        returnMessage = TranslationManager.GetTranslation(data.User.Lang, "commandPingDev", data.ChannelID)
                                    .Replace("%version%", BotEngine.botVersion)
                                    .Replace("%patch%", BotEngine.patchID)
                                    .Replace("%workTime%", TextUtil.FormatTimeSpan(workTime, data.User.Lang))
                                    .Replace("%tabs%", Bot.Channels.Length.ToString())
                                    .Replace("%loadedCMDs%", Bot.CommandsActive.ToString())
                                    .Replace("%completedCMDs%", BotEngine.completedCommands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString())
                                    .Replace("%tps%", BotEngine.tps.ToString())
                                    .Replace("%max_tps%", BotEngine.ticks.ToString())
                                    .Replace("%tick_delay%", BotEngine.tickDelay.ToString())
                                    .Replace("%tick_counted%", BotEngine.tickCounter.ToString())
                                    .Replace("%skiped_ticks%", BotEngine.tiksSkiped.ToString());
                    }

                    return new()
                    {
                        Message = returnMessage,
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = TranslationManager.GetTranslation(data.User.Lang, "dsPingTitle", data.ChannelID),
                        Color = Color.Green,
                        NickNameColor = ChatColorPresets.YellowGreen
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        Message = "",
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = "",
                        Color = Color.Green,
                        NickNameColor = ChatColorPresets.YellowGreen,
                        IsError = true,
                        Error = e
                    };
                }
            }
        }
    }
}
