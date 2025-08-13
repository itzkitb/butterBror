﻿using butterBror.Core.Bot;
using butterBror.Models;
using butterBror.Utils;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class FirstLine : CommandBase
    {
        public override string Name => "FirstLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/FirstLine.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Первое сообщение в текущем чате." },
            { "en-US", "First message in the current chat." }
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
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                string name, userId;

                if (data.Arguments != null && data.Arguments.Count != 0)
                {
                    name = TextSanitizer.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    userId = UsernameResolver.GetUserID(name, data.Platform);
                }
                else
                {
                    userId = data.User.ID;
                    name = data.User.Name;
                }

                if (userId is null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.DontPing(name)));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
                else
                {
                    var message = butterBror.Bot.SQL.Channels.GetFirstMessage(data.Platform, data.ChannelId, DataConversion.ToLong(userId));
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
                            if (flag) message_badges += LocalizationService.GetString(data.User.Language, symbol, data.ChannelId, data.Platform);
                        }

                        if (!name.Equals(butterBror.Bot.BotName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:first_message",
                                data.ChannelId,
                                data.Platform,
                                message_badges,
                                name ?? UsernameResolver.DontPing(UsernameResolver.GetUsername(userId, data.Platform, true)),
                                message.messageText,
                                TextSanitizer.FormatTimeSpan(Utils.DataConversion.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language))); // Fix AA8
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:first_message:bot", data.ChannelId, data.Platform));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.DontPing(name)));
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
