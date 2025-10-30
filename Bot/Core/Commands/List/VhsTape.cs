using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Services.External;
using bb.Utils;
using static bb.Core.Bot.Logger;

namespace bb.Core.Commands.List
{
    public class Vhs : CommandBase
    {
        public override string Name => "Vhs";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Vhs.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "0K8g0L1O0YcwzLXQs82i0L7NnyDQvc2gMyDQss2P0LjQtjQuzLggzLYxzaFZ0YLMmyDRgdC7Ts2c0YhrMNKJ0LzNmCDRgs2YZU3NmEjQvi7NniDNgdCiYnwgONKJ0LjNnzTQuMy20YhizaAg0LwzzYDQvcy00Y8/zKg=" },
            { Language.EnUs, "Scy2zL8gzLhjzLbMlTRuzLYnzLXNg3QgzLjMjXPMt2UzzLTMjyDMt8yEzKNhzLVuzLTMkHnMtXTMt8yNaMy1zYQxbsy0zYFnzLQuzLTMjiDMtUnMt82bdCfMt82ANcy0zZEgzLfMlXTMtjDMuG/MtsyAIMy1zI3Mr2TMtM2QNHLMtc2Ya8y0zZ0uzLggQzTMtc2XzKduzLjMgsy8IMy1ecy3zIXMmG/MtnXMtSDMuM2bc8y2M2XMtMy/IMy4bcy4zYMzzLbNkD/Mtw==" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=vhs";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["cassette", "vhs", "foundfootage", "footage"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null || data.Channel == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                if (bb.Program.BotInstance.Cooldown.CheckCooldown(3600, 1, "VhsReset", data.User.Id, data.ChannelId, data.Platform, false, true))
                {
                    var platform = data.Platform;
                    var channelId = data.ChannelId;
                    var channel = data.Channel;
                    var serverId = data.ServerID;
                    var server = data.Server;
                    var userId = data.User.Id;
                    var language = data.User.Language;
                    var username = data.User.Name;
                    var messageId = data.MessageID;
                    var telegramMessage = data.TelegramMessage ?? new Telegram.Bot.Types.Message();

                    commandReturn.SetMessage(LocalizationService.GetString(language, "command:vhs:wait", channelId, platform)); // fix AB4

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Random rand = new Random();
                            if (platform == Platform.Twitch)
                            {
                                await Task.Delay(rand.Next(10000, 30000));
                            }

                            var videos = new YouTubeService(new HttpClient()).GetPlaylistVideosAsync("https://www.youtube.com/playlist?list=PLAZUCud8HyO-9Ni4BSFkuBTOK8e3S5OLL").Result;
                            int index = rand.Next(videos.Length);
                            string randomUrl = videos[index];
                            string message = LocalizationService.GetString(language, "command:vhs", channelId, platform, randomUrl);

                            bb.Program.BotInstance.MessageSender.Send(platform, message, channel, channelId,
                                language, username, userId, server, serverId, messageId, telegramMessage, true, isReply: false, addUsername: true);
                        }
                        catch (Exception ex)
                        {
                            Write(ex);
                        }
                    });
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:vhs:wait_for_timeout", data.ChannelId, data.Platform));
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
