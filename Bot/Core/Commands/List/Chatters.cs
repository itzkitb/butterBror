using bb.Core.Bot;
using bb.Models;
using bb.Services.External;
using bb.Utils;

namespace bb.Core.Commands.List
{
    public class Chattes : CommandBase
    {
        public override string Name => "Chatters";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Chatters.cs";
        public override Version Version => new Version("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Выводит список чатеров." },
            { "en-US", "Displays a list of chatters." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=chatters";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["chatters", "chat", "чатеры", "чат"];
        public override string HelpArguments => "(none)";
        public override DateTime CreationDate => DateTime.Parse("28/07/2025");
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyBotModerator => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram];
        public override bool IsAsync => true;
        public override bool TechWorks => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                string targetChannel = data.Arguments.Count > 0
                    ? data.Arguments[0]
                    : data.Channel;

                string cursor = null;
                var allChatters = new List<TwitchLib.Api.Helix.Models.Chat.GetChatters.Chatter>();

                try
                {
                    do
                    {
                        var response = await bb.Bot.Clients.TwitchAPI.Helix.Chat.GetChattersAsync(
                            broadcasterId: UsernameResolver.GetUserID(targetChannel, PlatformsEnum.Twitch, true),
                            moderatorId: UsernameResolver.GetUserID(bb.Bot.BotName, PlatformsEnum.Twitch, true),
                            first: 100,
                            after: cursor
                        );

                        if (response?.Data != null)
                            allChatters.AddRange(response.Data);

                        cursor = response?.Pagination?.Cursor;
                    } while (!string.IsNullOrEmpty(cursor));
                }
                catch (Exception ex)
                {
                    Core.Bot.Console.Write(ex);
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, string.Empty, data.ChannelId, data.Platform));
                }

                var chattersText = $"Chatters ({allChatters.Count}):\n" +
                       string.Join("\n", allChatters.Select(c => c.UserLogin));

                var pasteUrl = await NoPasteService.Upload(chattersText, 86400);

                if (!string.IsNullOrEmpty(pasteUrl))
                {
                    commandReturn.SetMessage($"Полный список чатеров: {pasteUrl} (всего: {allChatters.Count})");
                }
                else
                {
                    commandReturn.SetMessage("❌ Не удалось загрузить список чатеров на nopaste.net");
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
