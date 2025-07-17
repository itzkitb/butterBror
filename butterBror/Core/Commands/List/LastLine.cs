using butterBror.Utils;
using butterBror.Data;
using butterBror.Models;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class LastLine : CommandBase
    {
        public override string Name => "LastLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/LastLine.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Последнее сообщение определенного пользователя в текущем чате." },
            { "en", "The last message of the selected user in the current chat." }
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
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.Arguments.Count != 0)
                {
                    var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    var userID = Names.GetUserID(name, PlatformsEnum.Twitch);
                    var message = MessagesWorker.GetMessage(data.ChannelID, userID, data.Platform);
                    var bages = "";
                    if (message != null)
                    {
                        if (userID != null)
                        {
                            if (name != Engine.Bot.BotName.ToLower())
                            {
                                if (name == data.User.Name.ToLower())
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
                                }
                                else
                                {
                                    var message_badges = "";
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
                                        if (flag) message_badges += TranslationManager.GetTranslation(data.User.Language, symbol, data.ChannelID, data.Platform);
                                    }
                                    var Date = message.messageDate;
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_message", data.ChannelID, data.Platform)
                                        .Replace("&timeAgo&", Text.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.Now, false), data.User.Language))
                                        .Replace("%message%", message.messageText).Replace("%bages%", message_badges)
                                        .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.Platform))));
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_line:bot", data.ChannelID, data.Platform));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                                .Replace("%user%", Names.DontPing(name)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                            .Replace("%user%", Names.DontPing(name))); // Fix AB1
                        commandReturn.SetColor(ChatColorPresets.Red); // Fix AB1
                    }
                }
                else
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
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