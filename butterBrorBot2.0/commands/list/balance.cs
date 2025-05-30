using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
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
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.arguments.Count == 0)
                    {
                        commandReturn.SetMessage(TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "command:balance", data.channel_id, data.platform),
                            "amount", Utils.Balance.GetBalance(data.user_id, data.platform) + "." + Utils.Balance.GetSubbalance(data.user_id, data.platform)));
                    }
                    else
                    {
                        var userID = Names.GetUserID(data.arguments[0].Replace("@", "").Replace(",", ""), data.platform);
                        if (userID != null)
                        {
                            commandReturn.SetMessage(TextUtil.ArgumentsReplacement(TranslationManager.GetTranslation(data.user.language, "command:balance:user", data.channel_id, data.platform),
                                new(){ { "amount", Utils.Balance.GetBalance(userID, data.platform) + "." + Utils.Balance.GetSubbalance(userID, data.platform) },
                                { "name", Names.DontPing(TextUtil.UsernameFilter(data.arguments_string)) }}));
                        }
                        else
                        {
                            commandReturn.SetMessage(TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform),
                                "user", Names.DontPing(TextUtil.UsernameFilter(data.arguments_string))));
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
