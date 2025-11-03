using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Services.External;
using bb.Utils;

namespace bb.Core.Commands.List.Utility
{
    public class Chatters : CommandBase
    {
        public override string Name => "Chatters";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Utility/Chatters.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Выводит список чатеров." },
            { Language.EnUs, "Displays a list of chatters." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["chatters", "chat", "чатеры", "чат"];
        public override string Help => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2025-07-28T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram];
        public override bool IsAsync => true;
        public override bool TechWorks => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null || data.Channel == null || Program.BotInstance.Clients.TwitchAPI == null ||
                    Program.BotInstance.TwitchName == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                string targetChannel = data.Arguments != null && data.Arguments.Count > 0
                    ? data.Arguments[0] : data.Channel;

                string? cursor = null;
                var allChatters = new List<TwitchLib.Api.Helix.Models.Chat.GetChatters.Chatter>();

                try
                {
                    do
                    {
                        var response = await Program.BotInstance.Clients.TwitchAPI.Helix.Chat.GetChattersAsync(
                            broadcasterId: UsernameResolver.GetUserID(targetChannel, Platform.Twitch, true),
                            moderatorId: UsernameResolver.GetUserID(Program.BotInstance.TwitchName, Platform.Twitch, true),
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
                    Bot.Logger.Write(ex);
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
