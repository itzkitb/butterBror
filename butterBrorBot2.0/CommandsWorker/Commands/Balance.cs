using butterBib;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Balance
        {
            public static CommandInfo Info = new()
            {
                Name = "Balance",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "При помощи этой команды вы можете посмотреть свой баланс.",
                UseURL = "https://itzkitb.ru/bot_command/wallet",
                UserCooldown = 10,
                GlobalCooldown = 1,
                aliases = ["balance", "баланс", "bal", "бал", "кошелек", "wallet"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string result = "";
                Color colords = Color.Green;
                ChatColorPresets colorNickname = ChatColorPresets.YellowGreen;
                if (TextUtil.FilterTextWithoutSpaces(data.ArgsAsString) == "")
                {
                    result = TranslationManager.GetTranslation(data.User.Lang, "balance", data.ChannelID)
                            .Replace("%coins%", UsersData.UserGetData<int>(data.UserUUID, "balance") + "." + UsersData.UserGetData<int>(data.UserUUID, "floatBalance"));
                }
                else
                {
                    var userID = NamesUtil.GetUserID(TextUtil.NicknameFilter(data.ArgsAsString));
                    if (userID != "err")
                    {
                        result = TranslationManager.GetTranslation(data.User.Lang, "balanceSelectedUser", data.ChannelID)
                            .Replace("%coins%", UsersData.UserGetData<int>(userID, "balance") + "." + UsersData.UserGetData<int>(userID, "floatBalance"))
                            .Replace("%name%", NamesUtil.DontPingUsername(TextUtil.NicknameFilter(data.ArgsAsString)));
                    }
                    else
                    {
                        result = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                            .Replace("%user%", NamesUtil.DontPingUsername(TextUtil.NicknameFilter(data.ArgsAsString)));
                        colords = Color.Red;
                        colorNickname = ChatColorPresets.Red;
                    }
                }
                return new()
                {
                    Message = result,
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = true,
                    Ephemeral = false,
                    Title = "",
                    Color = colords,
                    NickNameColor = colorNickname
                };
            }
        }
    }
}
