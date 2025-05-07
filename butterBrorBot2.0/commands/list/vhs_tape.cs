using TwitchLib.Client.Enums;
using butterBror.Utils;
using Discord;
using butterBror;

namespace butterBror
{
    public partial class Commands
    {
        public class Vhs
        {
            public static CommandInfo Info = new()
            {
                name = "Vhs",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "0K8g0L1O0YcwzLXQs82i0L7NnyDQvc2gMyDQss2P0LjQtjQuzLggzLYxzaFZ0YLMmyDRgdC7Ts2c0YhrMNKJ0LzNmCDRgs2YZU3NmEjQvi7NniDNgdCiYnwgONKJ0LjNnzTQuMy20YhizaAg0LwzzYDQvcy00Y8/zKg=" },
                    { "en", "Scy2zL8gzLhjzLbMlTRuzLYnzLXNg3QgzLjMjXPMt2UzzLTMjyDMt8yEzKNhzLVuzLTMkHnMtXTMt8yNaMy1zYQxbsy0zYFnzLQuzLTMjiDMtUnMt82bdCfMt82ANcy0zZEgzLfMlXTMtjDMuG/MtsyAIMy1zI3Mr2TMtM2QNHLMtc2Ya8y0zZ0uzLggQzTMtc2XzKduzLjMgsy8IMy1ecy3zIXMmG/MtnXMtSDMuM2bc8y2M2XMtMy/IMy4bcy4zYMzzLbNkD/Mtw==" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=vhs",
                cooldown_per_user = 60,
                cooldown_global = 10,
                aliases = ["cassette", "vhs", "foundfootage", "footage"],
                arguments = string.Empty,
                cooldown_reset = false,
                creation_date = DateTime.Parse("07/04/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public async Task<CommandReturn> Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    if (Command.CheckCooldown(3600, 1, "vhs_reset", data.user_id, data.channel_id, data.platform, false, true, true))
                    {
                        // Сохраняем необходимые данные для использования в фоновой задаче
                        var platform = data.platform;
                        var channelId = data.channel_id;
                        var channel = data.channel;
                        var userId = data.user_id;
                        var language = data.user.language;
                        var username = data.user.username;

                        // Запускаем фоновую задачу без ожидания
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Random rand = new Random();
                                if (platform == Platforms.Twitch)
                                {
                                    await Task.Delay(rand.Next(10000, 30000)); // Асинхронная задержка
                                }

                                var videos = YouTube.GetPlaylistVideos("https://www.youtube.com/playlist?list=PLAZUCud8HyO-9Ni4BSFkuBTOK8e3S5OLL");
                                int index = rand.Next(videos.Length);
                                string randomUrl = videos[index];

                                // Создаем данные для отправки на основе платформы
                                switch (platform)
                                {
                                    case Platforms.Twitch:
                                        TwitchMessageSendData twitchData = new()
                                        {
                                            message = TranslationManager.GetTranslation(language, "command:vhs", channelId, platform).Replace("%url%", randomUrl),
                                            channel = channel,
                                            channel_id = channelId,
                                            message_id = data.twitch_arguments?.Command.ChatMessage.Id,
                                            language = language,
                                            username = username,
                                            safe_execute = true,
                                            nickname_color = ChatColorPresets.YellowGreen
                                        };
                                        SendCommandReply(twitchData);
                                        break;

                                    case Platforms.Discord:
                                        DiscordCommandSendData discordData = new()
                                        {
                                            message = TranslationManager.GetTranslation(language, "command:vhs", channelId, platform).Replace("%url%", randomUrl),
                                            description = "",
                                            is_embed = false,
                                            is_ephemeral = false,
                                            server = channel,
                                            server_id = channelId,
                                            language = language,
                                            safe_execute = true,
                                            socket_command_base = data.discord_command_base
                                        };
                                        SendCommandReply(discordData);
                                        break;

                                    case Platforms.Telegram:
                                        TelegramMessageSendData telegramData = new()
                                        {
                                            message = TranslationManager.GetTranslation(language, "command:vhs", channelId, platform).Replace("%url%", randomUrl),
                                            channel = channel,
                                            channel_id = channelId,
                                            message_id = data.twitch_arguments?.Command.ChatMessage.Id,
                                            language = language,
                                            username = username,
                                            safe_execute = true
                                        };
                                        SendCommandReply(telegramData);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Utils.Console.WriteError(ex, "vhs_bg_task");
                            }
                        });

                        return new()
                        {
                            message = TranslationManager.GetTranslation(data.user.language, "command:vhs:wait", data.channel_id, data.platform),
                            safe_execute = true,
                            description = "",
                            author = "",
                            image_link = "",
                            thumbnail_link = "",
                            footer = "",
                            is_embed = true,
                            is_ephemeral = false,
                            title = "",
                            embed_color = global::Discord.Color.Blue,
                            nickname_color = ChatColorPresets.DodgerBlue
                        };
                    }
                    else
                    {
                        return new()
                        {
                            message = TranslationManager.GetTranslation(data.user.language, "command:vhs:wait_for_timeout", data.channel_id, data.platform),
                            safe_execute = true,
                            description = "",
                            author = "",
                            image_link = "",
                            thumbnail_link = "",
                            footer = "",
                            is_embed = true,
                            is_ephemeral = false,
                            title = "",
                            embed_color = global::Discord.Color.Red,
                            nickname_color = ChatColorPresets.Red
                        };
                    }
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
