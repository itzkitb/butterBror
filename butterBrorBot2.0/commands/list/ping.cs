using System.Net.NetworkInformation;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror;

namespace butterBror
{
    public partial class Commands
    {
        public class Pinger
        {
            public static CommandInfo Info = new()
            {
                name = "Ping",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Пинг с Вояджер-1: 243000000 мс" }, 
                    { "en", "Ping to Voyager-1: 243000000 ms" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=ping",
                cooldown_per_user = 30,
                cooldown_global = 5,
                aliases = ["ping", "пинг", "понг", "пенг", "п"],
                arguments = "(ISP/dev)",
                cooldown_reset = true,
                creation_date = DateTime.Parse("07/04/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string arg1 = "";
                    if (data.arguments.Count > 0)
                        arg1 = data.arguments[0].ToLower();

                    string returnMessage = "";
                    if (data.arguments.Count == 0)
                    {
                        var workTime = DateTime.Now - Engine.start_time;
                        string host = "";
                        if (data.platform == Platforms.Discord) host = "discord.com";
                        else if (data.platform == Platforms.Twitch) host = "twitch.tv";
                        else if (data.platform == Platforms.Telegram) host = "t.me";
                        PingReply reply = new System.Net.NetworkInformation.Ping().Send(host, 1000);
                        long pingSpeed = -1;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;

                        returnMessage = TranslationManager.GetTranslation(data.user.language, "command:ping", data.channel_id, data.platform)
                                    .Replace("%version%", Engine.version)
                                    .Replace("%workTime%", TextUtil.FormatTimeSpan(workTime, data.user.language))
                                    .Replace("%tabs%", Maintenance.channels_list.Length.ToString())
                                    .Replace("%loadedCMDs%", Commands.commands.Count.ToString())
                                    .Replace("%completedCMDs%", Engine.completed_commands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString());
                    }
                    else if (arg1.Equals("isp"))
                    {
                        var workTime = DateTime.Now - Engine.start_time;
                        PingReply reply = new System.Net.NetworkInformation.Ping().Send("192.168.1.1", 1000);
                        long pingSpeed = -1;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        else
                        {
                            reply = new System.Net.NetworkInformation.Ping().Send("192.168.0.1", 1000);
                            if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        }
                        
                        returnMessage = TranslationManager.GetTranslation(data.user.language, "command:ping:isp", data.channel_id, data.platform)
                                    .Replace("%ping%", pingSpeed.ToString());
                    }
                    else if (arg1.Equals("dev"))
                    {
                        var workTime = DateTime.Now - Engine.start_time;
                        string host = "";
                        if (data.platform == Platforms.Discord) host = "discord.com";
                        else if (data.platform == Platforms.Twitch) host = "twitch.tv";
                        else if (data.platform == Platforms.Telegram) host = "t.me";

                        PingReply reply = new System.Net.NetworkInformation.Ping().Send(host, 1000);
                        long pingSpeed = 0;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;

                        returnMessage = TranslationManager.GetTranslation(data.user.language, "command:ping:development", data.channel_id, data.platform)
                                    .Replace("%version%", Engine.version)
                                    .Replace("%patch%", Engine.patch)
                                    .Replace("%workTime%", TextUtil.FormatTimeSpan(workTime, data.user.language))
                                    .Replace("%tabs%", Maintenance.channels_list.Length.ToString())
                                    .Replace("%loadedCMDs%", Commands.commands.Count.ToString())
                                    .Replace("%completedCMDs%", Engine.completed_commands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString())
                                    .Replace("%tps%", Engine.ticks_per_second.ToString())
                                    .Replace("%max_tps%", Engine.ticks.ToString())
                                    .Replace("%tick_delay%", Engine.tick_delay.ToString())
                                    .Replace("%tick_counted%", Engine.ticks_counter.ToString())
                                    .Replace("%skiped_ticks%", Engine.skipped_ticks.ToString());
                    }

                    return new()
                    {
                        message = returnMessage,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = TranslationManager.GetTranslation(data.user.language, "discord:ping:title", data.channel_id, data.platform),
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
        }
    }
}
