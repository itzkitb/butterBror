using bb.Core.Bot;
using bb.Models;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class LastLine : CommandBase
    {
        public override string Name => "LastLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/LastLine.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Последнее сообщение определенного пользователя в текущем чате." },
            { "en-US", "The last message of the selected user in the current chat." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=ll";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["ll", "lastline", "пс", "последнеесообщение"];
        public override string HelpArguments => "[name]";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.Arguments.Count != 0)
                {
                    var name = TextSanitizer.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    var userId = UsernameResolver.GetUserID(name, PlatformsEnum.Twitch, true);
                    var message = userId is null ? null : bb.Bot.DataBase.Messages.GetMessage(data.Platform, data.ChannelId, DataConversion.ToLong(userId), 0);

                    if (message != null && userId != null && name != bb.Bot.Name.ToLower())
                    {
                        if (name == data.User.Name.ToLower())
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "text:you_right_there", data.ChannelId, data.Platform));
                        }
                        else
                        {
                            var message_badges = string.Empty;
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
                                if (flag) message_badges += LocalizationService.GetString(data.User.Language, symbol, data.ChannelId, data.Platform);
                            }

                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:last_message",
                                data.ChannelId,
                                data.Platform,
                                message_badges,
                                UsernameResolver.Unmention(UsernameResolver.GetUsername(userId, data.Platform, true)),
                                message.messageText,
                                TextSanitizer.FormatTimeSpan(Utils.DataConversion.GetTimeTo(message.messageDate, DateTime.Now, false), data.User.Language)));
                        }
                    }
                    else if (name != bb.Bot.Name.ToLower())
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:last_line:bot", data.ChannelId, data.Platform));
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(name))); // Fix AB1
                        commandReturn.SetColor(ChatColorPresets.Red); // Fix AB1
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "text:you_right_there", data.ChannelId, data.Platform));
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