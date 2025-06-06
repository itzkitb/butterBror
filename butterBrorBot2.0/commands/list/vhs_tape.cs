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
                Name = "Vhs",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "0K8g0L1O0YcwzLXQs82i0L7NnyDQvc2gMyDQss2P0LjQtjQuzLggzLYxzaFZ0YLMmyDRgdC7Ts2c0YhrMNKJ0LzNmCDRgs2YZU3NmEjQvi7NniDNgdCiYnwgONKJ0LjNnzTQuMy20YhizaAg0LwzzYDQvcy00Y8/zKg=" },
                    { "en", "Scy2zL8gzLhjzLbMlTRuzLYnzLXNg3QgzLjMjXPMt2UzzLTMjyDMt8yEzKNhzLVuzLTMkHnMtXTMt8yNaMy1zYQxbsy0zYFnzLQuzLTMjiDMtUnMt82bdCfMt82ANcy0zZEgzLfMlXTMtjDMuG/MtsyAIMy1zI3Mr2TMtM2QNHLMtc2Ya8y0zZ0uzLggQzTMtc2XzKduzLjMgsy8IMy1ecy3zIXMmG/MtnXMtSDMuM2bc8y2M2XMtMy/IMy4bcy4zYMzzLbNkD/Mtw==" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=vhs",
                CooldownPerUser = 60,
                CooldownPerChannel = 10,
                Aliases = ["cassette", "vhs", "foundfootage", "footage"],
                Arguments = string.Empty,
                CooldownReset = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public async Task<CommandReturn> Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (Command.CheckCooldown(3600, 1, "vhs_reset", data.user_id, data.channel_id, data.platform, false, true, true))
                    {
                        var platform = data.platform;
                        var channelId = data.channel_id;
                        var channel = data.channel;
                        var serverId = data.server_id;
                        var server = data.server;
                        var userId = data.user_id;
                        var language = data.user.language;
                        var username = data.user.username;
                        var messageId = data.message_id;
                        var telegramMessage = data.telegram_message;

                        commandReturn.SetMessage(TranslationManager.GetTranslation(language, "command:vhs:wait", channelId, platform)); // fix AB4

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Random rand = new Random();
                                if (platform == Platforms.Twitch)
                                {
                                    await Task.Delay(rand.Next(10000, 30000));
                                }

                                var videos = YouTube.GetPlaylistVideos("https://www.youtube.com/playlist?list=PLAZUCud8HyO-9Ni4BSFkuBTOK8e3S5OLL");
                                int index = rand.Next(videos.Length);
                                string randomUrl = videos[index];

                                Chat.SendReply(platform, channel, channelId, TranslationManager.GetTranslation(language, "command:vhs", channelId, platform).Replace("%url%", randomUrl),
                                    language, username, userId, server, serverId, messageId, telegramMessage, true);
                            }
                            catch (Exception ex)
                            {
                                Utils.Console.WriteError(ex, "vhs_bg_task");
                            }
                        });
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:vhs:wait_for_timeout", data.channel_id, data.platform));
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
