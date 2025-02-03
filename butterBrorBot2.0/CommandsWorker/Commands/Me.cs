using butterBror.Utils;
using butterBib;
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
                Name = "Me",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда... Просто зачем-то существует.",
                UseURL = "https://itzkitb.ru/bot/command?name=me",
                UserCooldown = 15,
                GlobalCooldown = 5,
                aliases = ["me", "m", "я"],
                ArgsRequired = "[текст]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    bool checked_msg = false;
                    string resultMessage = "";
                    if (TextUtil.CleanAsciiWithoutSpaces(data.ArgsAsString) != "")
                    {
                        string[] blockedEntries = ["/", "$", "#", "+", "-"];
                        string meMessage = TextUtil.CleanAscii(data.ArgsAsString);
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
                        resultMessage = "/me " + TranslationManager.GetTranslation(data.User.Lang, "YouCanPlaceAdHere", data.ChannelID);
                    }
                    return new()
                    {
                        Message = resultMessage,
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
                        NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
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