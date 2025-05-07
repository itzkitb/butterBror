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
                name = "Balance",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "При помощи этой команды вы можете посмотреть свой баланс." },
                    { "en", "With this command you can view your balance." } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=wallet",
                cooldown_per_user = 10,
                cooldown_global = 1,
                aliases = ["balance", "баланс", "bal", "бал", "кошелек", "wallet"],
                arguments = string.Empty,
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
                    string result = "";
                    Color colords = Color.Green;
                    ChatColorPresets colorNickname = ChatColorPresets.YellowGreen;
                    if (data.arguments.Count == 0)
                    {
                        result = TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "command:balance", data.channel_id, data.platform),
                            "amount", Utils.Balance.GetBalance(data.user_id, data.platform) + "." + Utils.Balance.GetBalanceFloat(data.user_id, data.platform));
                    }
                    else
                    {
                        var userID = Names.GetUserID(data.arguments[0].Replace("@", "").Replace(",", ""), data.platform);
                        if (userID != null)
                        {
                            result = TextUtil.ArgumentsReplacement(TranslationManager.GetTranslation(data.user.language, "command:balance:user", data.channel_id, data.platform),
                                new(){ { "amount", Utils.Balance.GetBalance(userID, data.platform) + "." + Utils.Balance.GetBalanceFloat(userID, data.platform) },
                                { "name", Names.DontPing(TextUtil.UsernameFilter(data.arguments_string)) }});
                        }
                        else
                        {
                            result = TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform),
                                "user", Names.DontPing(TextUtil.UsernameFilter(data.arguments_string)));
                            colords = Color.Red;
                            colorNickname = ChatColorPresets.Red;
                        }
                    }

                    return new()
                    {
                        message = result,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = colords,
                        nickname_color = colorNickname
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
