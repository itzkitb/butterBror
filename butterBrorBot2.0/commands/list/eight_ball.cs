using butterBror.Utils;
using butterBror;
using Discord;
using Discord.Rest;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Eightball
        {
            public static CommandInfo Info = new()
            {
                name = "EightBall",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Будущее пугает..." },
                    { "en", "The future is scary..." }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=8ball",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["8ball", "eightball", "eb", "8b", "шар"],
                arguments = string.Empty,
                cooldown_reset = true,
                creation_date = DateTime.Parse("08/08/2024"),
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
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    Random rand = new Random();
                    int stage1 = rand.Next(1, 5);
                    int stage2 = rand.Next(1, 6);
                    string translationParam = "command:8ball:";
                    if (stage1 == 1)
                    {
                        resultNicknameColor = ChatColorPresets.DodgerBlue;
                        resultColor = Color.Blue;
                        translationParam += "positively:" + stage2;
                    }
                    else if (stage1 == 2)
                    {
                        translationParam += "hesitantly:" + stage2;
                    }
                    else if (stage1 == 3)
                    {
                        resultNicknameColor = ChatColorPresets.GoldenRod;
                        resultColor = Color.Gold;
                        translationParam += "neutral:" + stage2;
                    }
                    else if (stage1 == 4)
                    {
                        resultNicknameColor = ChatColorPresets.Red;
                        resultColor = Color.Red;
                        translationParam += "negatively:" + stage2;
                    }
                    resultMessage = "🔮 " + TranslationManager.GetTranslation(data.user.language, translationParam, data.channel_id, data.platform);
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
                        embed_color = resultColor,
                        nickname_color = resultNicknameColor
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