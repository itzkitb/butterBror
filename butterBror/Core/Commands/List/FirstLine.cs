using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class FirstLine : CommandBase
    {
        public override string Name => "FirstLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/FirstLine.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Первое сообщение в текущем чате." },
            { "en", "First message in the current chat." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=fl";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["fl", "firstline", "прс", "первоесообщение"];
        public override string HelpArguments => "(name)";
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
                if (data.Arguments != null && data.Arguments.Count != 0)
                {
                    var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    var userID = Names.GetUserID(name, data.Platform);

                    if (userID is null)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                            .Replace("%user%", Names.DontPing(name)));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    else
                    {
                        var message = MessagesWorker.GetMessage(data.ChannelID, userID, data.Platform, true, -1);
                        var message_badges = string.Empty;
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
                                if (flag) message_badges += TranslationManager.GetTranslation(data.User.Language, symbol, data.ChannelID, data.Platform);
                            }

                            if (!name.Equals(Engine.Bot.BotName, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (name == data.User.Name)
                                {
                                    commandReturn.SetMessage(Text.ArgumentsReplacement(
                                        TranslationManager.GetTranslation(data.User.Language, "command:first_message:user", data.ChannelID, data.Platform), new() { // Fix AA8
                                            { "ago", Text.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges } }));
                                }
                                else
                                {
                                    commandReturn.SetMessage(Text.ArgumentsReplacement(
                                        TranslationManager.GetTranslation(data.User.Language, "command:first_message", data.ChannelID, data.Platform), new() { // Fix AA8
                                            { "ago", Text.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges },
                                            { "user", Names.DontPing(Names.GetUsername(userID, data.Platform)) } }));
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_message:bot", data.ChannelID, data.Platform));
                            }
                        }
                    }
                }
                else
                {
                    var user_id = data.UserID;
                    var message = MessagesWorker.GetMessage(data.ChannelID, user_id, data.Platform, true, -1);
                    var message_badges = "";
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
                            if (flag) message_badges += TranslationManager.GetTranslation(data.User.Language, symbol, data.ChannelID, data.Platform);
                        }
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_message:user", data.ChannelID, data.Platform)
                            .Replace("%ago%", Text.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language))
                            .Replace("%message%", message.messageText).Replace("%bages%", message_badges));
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
