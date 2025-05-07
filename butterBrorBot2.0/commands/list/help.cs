using butterBror.Utils;
using butterBror;
using Discord;
using System.Reflection;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Help
        {
            public static CommandInfo Info = new()
            {
                name = "Help",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Чувак, ты думал здесь что-то будет?" }, 
                    { "en", "Dude, did you think something was going to happen here?" } 
                },
                wiki_link = "https://itzkitb.ru/bot/command?name=help",
                cooldown_per_user = 120,
                cooldown_global = 10,
                aliases = ["help", "info", "помощь", "hlp"],
                arguments = "(command name)",
                cooldown_reset = false,
                creation_date = DateTime.Parse("09/12/2024"),
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
                    string result = "";
                    if (data.arguments.Count == 1)
                    {
                        string classToFind = data.arguments[0];
                        result = TranslationManager.GetTranslation(data.user.language, "command:help:not_found", data.channel_id, data.platform);
                        foreach (var classType in Commands.commands)
                        {
                            var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                            var info = infoProperty.GetValue(null) as CommandInfo;

                            if (info.aliases.Contains(classToFind))
                            {
                                string aliasesList = "";
                                int num = 0;
                                int numWithoutComma = 5;
                                if (info.aliases.Length < 5)
                                    numWithoutComma = info.aliases.Length;

                                foreach (string alias in info.aliases)
                                {
                                    num++;
                                    if (num < numWithoutComma)
                                        aliasesList += $"#{alias}, ";
                                    else if (num == numWithoutComma)
                                        aliasesList += $"#{alias}";
                                }
                                result = TranslationManager.GetTranslation(data.user.language, "command:help", data.channel_id, data.platform)
                                    .Replace("%commandName%", info.name)
                                    .Replace("%Variables%", aliasesList)
                                    .Replace("%Args%", info.arguments)
                                    .Replace("%Link%", info.wiki_link)
                                    .Replace("%Description%", info.description[data.user.language])
                                    .Replace("%Author%", Names.DontPing(info.author))
                                    .Replace("%creationDate%", info.creation_date.ToShortDateString())
                                    .Replace("%uCooldown%", info.cooldown_per_user.ToString())
                                    .Replace("%gCooldown%", info.cooldown_global.ToString());

                                break;
                            }
                        }
                    }
                    else if (data.arguments.Count > 1)
                        result = TranslationManager.GetTranslation(data.user.language, "error:a_few_arguments", data.channel_id, data.platform).Replace("%args%", "(command_name)");
                    else
                        result = TranslationManager.GetTranslation(data.user.language, "text:bot_info", data.channel_id, data.platform);


                    return new()
                    {
                        message = result,
                        safe_execute = true,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = TranslationManager.GetTranslation(data.user.language, "discord:winter:title", data.channel_id, data.platform),
                        embed_color = Color.Blue,
                        nickname_color = ChatColorPresets.DodgerBlue
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
