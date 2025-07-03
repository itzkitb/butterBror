using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "При помощи этой команды вы можете посмотреть свой баланс." },
                    { "en", "With this command you can view your balance." } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=wallet",
                CooldownPerUser = 10,
                CooldownPerChannel = 1,
                Aliases = ["balance", "баланс", "bal", "бал", "кошелек", "wallet"],
                Arguments = string.Empty,
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.Arguments.Count == 0)
                    {
                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "command:balance", data.ChannelID, data.Platform),
                            "amount", Utils.Tools.Balance.GetBalance(data.UserID, data.Platform) + "." + Utils.Tools.Balance.GetSubbalance(data.UserID, data.Platform)));
                    }
                    else
                    {
                        var userID = Names.GetUserID(data.Arguments[0].Replace("@", "").Replace(",", ""), data.Platform);
                        if (userID != null)
                        {
                            commandReturn.SetMessage(Text.ArgumentsReplacement(TranslationManager.GetTranslation(data.User.Language, "command:balance:user", data.ChannelID, data.Platform),
                                new(){ { "amount", Utils.Tools.Balance.GetBalance(userID, data.Platform) + "." + Utils.Tools.Balance.GetSubbalance(userID, data.Platform) },
                                { "name", Names.DontPing(Text.UsernameFilter(data.ArgumentsString)) }}));
                        }
                        else
                        {
                            commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform),
                                "user", Names.DontPing(Text.UsernameFilter(data.ArgumentsString))));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }
        }
    }
}
