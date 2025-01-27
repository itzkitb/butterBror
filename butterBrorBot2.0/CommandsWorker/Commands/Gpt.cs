using butterBror.Utils;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class GPT
        {
            public static CommandInfo Info = new()
            {
                Name = "GPT",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "При помощи этой команды вы можете задать вопрос нейросети GPT 3.5 turbo.",
                UseURL = "https://itzkitb.ru/bot/command?name=gpt",
                UserCooldown = 30,
                GlobalCooldown = 10,
                aliases = ["gpt", "гпт"],
                ArgsRequired = "(Вопрос)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("04/07/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                Cost = 5.0,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static async Task<CommandReturn> Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    string resultMessageTitle = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    if (NoBanwords.fullCheck(data.ArgsAsString, data.ChannelID))
                    {
                        BalanceUtil.SaveBalance(data.UserUUID, -5, 0);
                        string[] result = await Utils.APIUtil.GPT.GPTRequest(data);
                        if (result.ElementAt(0) == "ERR")
                        {
                            resultMessage = "🚩 " + TranslationManager.GetTranslation(data.User.Lang, "gptERR", data.ChannelID);
                            resultNicknameColor = ChatColorPresets.Red;
                            resultColor = Color.Red;
                        }
                        else
                        {
                            if (NoBanwords.fullCheck(result.ElementAt(0), data.ChannelID))
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "gptSuccess", data.ChannelID).Replace("%text%", result.ElementAt(0)).Replace("%model%", result.ElementAt(1).Replace("-", " "));
                            }
                            else
                            {
                                return null;
                            }
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
                            IsEmbed = false,
                            Ephemeral = false,
                            Title = resultMessageTitle,
                            Color = resultColor,
                            NickNameColor = resultNicknameColor
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception e)
                {
                    return new ()
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
