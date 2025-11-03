using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List.ChatLines
{
    public class FirstLine : CommandBase
    {
        public override string Name => "FirstLine";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "ChatLines/FirstLine.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Первое сообщение в текущем чате." },
            { Language.EnUs, "First message in the current chat." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["fl", "firstline", "прс", "первоесообщение"];
        public override string Help => "<name>";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null || Program.BotInstance.DataBase == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                string name, userId;

                if (data.Arguments != null && data.Arguments.Count != 0)
                {
                    name = TextSanitizer.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    userId = UsernameResolver.GetUserID(name, data.Platform);
                }
                else
                {
                    userId = data.User.Id;
                    name = data.User.Name;
                }

                if (userId is null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(name)));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
                else
                {
                    var message = Program.BotInstance.DataBase.Channels.GetFirstMessage(data.Platform, data.ChannelId, DataConversion.ToLong(userId));
                    var messageBadges = string.Empty;
                    if (message != null)
                    {
                        var badges = new (bool flag, string symbol)[]
                        {
                            (message.isMe, "symbol:splash_me"),
                            (message.isVip, "symbol:vip"),
                            (message.isTurbo, "symbol:turbo"),
                            (message.isModerator, "symbol:moderator"),
                            (message.isPartner, "symbol:partner"),
                            (message.isStaff, "symbol:staff"),
                            (message.isSubscriber, "symbol:subscriber")
                        };

                        foreach (var (flag, symbol) in badges)
                        {
                            if (flag) messageBadges += LocalizationService.GetString(data.User.Language, symbol, data.ChannelId, data.Platform);
                        }

                        if (!name.Equals(Program.BotInstance.TwitchName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:first_message",
                                data.ChannelId,
                                data.Platform,
                                messageBadges,
                                name ?? UsernameResolver.Unmention(UsernameResolver.GetUsername(userId, data.Platform, true)),
                                message.messageText,
                                TextSanitizer.FormatTimeSpan(DataConversion.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language))); // Fix AA8
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:first_message:bot", data.ChannelId, data.Platform));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(name)));
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
