using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;


namespace butterBror
{
    public partial class Commands
    {
        public class Me
        {
            public static CommandInfo Info = new()
            {
                name = "Me",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Эта команда... Просто зачем-то существует" },
                    { "en", "This command... Just exists for some reason" }
                },
                wiki_link = "https://itzkitb.ru/bot/command?name=me",
                cooldown_per_user = 15,
                cooldown_global = 5,
                aliases = ["me", "m", "я"],
                arguments = "[text]",
                cooldown_reset = true,
                creation_date = DateTime.Parse("07/04/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    bool checked_msg = false;
                    string resultMessage = "";
                    if (TextUtil.CleanAsciiWithoutSpaces(data.arguments_string) != "")
                    {
                        string[] blockedEntries = ["/", "$", "#", "+", "-"];
                        string meMessage = TextUtil.CleanAscii(data.arguments_string);
                        while (!checked_msg)
                        {
                            checked_msg = true;
                            while (("\n" + meMessage).Contains("\n "))
                            {
                                meMessage = ("\n" + meMessage).Replace("\n ", "");
                            }
                            if (("\n" + meMessage).Contains("\n!"))
                            {
                                meMessage = ("\n" + meMessage).Replace("\n!", "❗");
                                checked_msg = false;
                            }
                            foreach (string blockedEntry in blockedEntries)
                            {
                                if (("\n" + meMessage).Contains($"\n{blockedEntry}"))
                                {
                                    meMessage = ("\n" + meMessage).Replace($"\n{blockedEntry}", "");
                                    checked_msg = false;
                                }
                            }
                        }
                        resultMessage = $"/me ⁣ {meMessage}";
                    }
                    else
                    {
                        resultMessage = "/me " + TranslationManager.GetTranslation(data.user.language, "text:ad", data.channel_id, data.platform);
                    }
                    return new()
                    {
                        message = resultMessage,
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
                        nickname_color = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
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