using bb.Core.Bot;
using bb.Core.Bot.SQLColumnNames;
using bb.Models;
using bb.Utils;
using System.Globalization;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class LastGlobalLine : CommandBase
    {
        public override string Name => "LastGlobalLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/LastGlobalLine.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Последнее сообщение определенного пользователя." },
            { "en-US", "The last message of the selected user." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=lgl";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"];
        public override string HelpArguments => "[name]";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.Arguments.Count != 0)
                {
                    string name = TextSanitizer.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    string userID = UsernameResolver.GetUserID(name, PlatformsEnum.Twitch, true);

                    if (userID == null)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(name)));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    else
                    {
                        string lastLine = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.LastMessage);
                        string lastChannel = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.LastChannel);
                        DateTime lastLineDate = DateTime.Parse((string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.LastSeen), null, DateTimeStyles.AdjustToUniversal);

                        if (name == bb.Bot.BotName.ToLower())
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:last_global_line:bot", data.ChannelId, data.Platform));
                        }
                        else if (name == data.User.Name.ToLower())
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "text:you_right_there", data.ChannelId, data.Platform));
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:last_global_line",
                                data.ChannelId,
                                data.Platform,
                                UsernameResolver.Unmention(UsernameResolver.GetUsername(userID, data.Platform, true)),
                                lastLine,
                                TextSanitizer.FormatTimeSpan(Utils.DataConversion.GetTimeTo(lastLineDate, DateTime.UtcNow, false), data.User.Language),
                                lastChannel));
                        }
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
