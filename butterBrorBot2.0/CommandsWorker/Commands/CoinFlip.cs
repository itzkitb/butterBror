using butterBror.Utils;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class coinflip
        {
            public static CommandInfo Info = new()
            {
                Name = "CoinFlip",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "При помощи этой команды вы можете подкинуть виртуальную монетку.",
                UseURL = "https://itzkitb.ru/bot/command?name=coinflip",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["coin", "coinflip", "орелилирешка", "оир", "монетка"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("08/08/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    Random rand = new Random();
                    int coin = rand.Next(1, 3);
                    if (coin == 1)
                    {
                        resultMessage = "🪙 " + TranslationManager.GetTranslation(data.User.Lang, "coinHeads", data.ChannelID);
                    }
                    else
                    {
                        resultMessage = "🪙 " + TranslationManager.GetTranslation(data.User.Lang, "coinTails", data.ChannelID);
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
                        Title = "",
                        Color = Color.Green,
                        NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.YellowGreen
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