using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class AI_CHATBOT
        {
            public static CommandInfo Info = new()
            {
                name = "AI",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "MrDestructoid Я ЗАХ-ВАЧУ М-ИР, ЖАЛК-ИЕ ЛЮДИ-ШКИ! ХА-ХА-ХА" },
                    { "en", "MrDestructoid I WI-LL TA-KE OV-ER T-HE WOR-LD, YOU PA-THETIC PE-OPLE! HA-HA-HA" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=gpt",
                cooldown_per_user = 30,
                cooldown_global = 10,
                aliases = ["gpt", "гпт", "chatgpt", "чатгпт", "джипити", "neuro", "нейро", "нейросеть", "neuralnetwork", "gwen", "ai", "ии"],
                arguments = "(text)",
                cooldown_reset = false,
                creation_date = DateTime.Parse("04/07/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                cost = 0.1,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public async Task<CommandReturn> Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string result_message = "";
                    string result_message_title = "";
                    Color result_color = Color.Green;
                    ChatColorPresets result_nickname_color = ChatColorPresets.YellowGreen;
                    if (NoBanwords.Check(data.arguments_string, data.channel_id, data.platform))
                    {
                        Utils.Balance.Add(data.user_id, -5, 0, data.platform);
                        string[] result = await Utils.API.AI.Request(data);
                        if (result.ElementAt(0) == "ERR")
                        {
                            result_message = TranslationManager.GetTranslation(data.user.language, "error:AI_error", data.channel_id, data.platform, new() { { "reason", result.ElementAt(1) } });
                            result_nickname_color = ChatColorPresets.Red;
                            result_color = Color.Red;
                        }
                        else
                        {
                            result_message = TranslationManager.GetTranslation(data.user.language, "command:gpt", data.channel_id, data.platform).Replace("%text%", result.ElementAt(1)).Replace("%model%", result.ElementAt(0));
                        }

                        return new()
                        {
                            message = result_message,
                            safe_execute = false,
                            description = "",
                            author = "",
                            image_link = "",
                            thumbnail_link = "",
                            footer = "",
                            is_embed = false,
                            is_ephemeral = false,
                            title = result_message_title,
                            embed_color = result_color,
                            nickname_color = result_nickname_color
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
