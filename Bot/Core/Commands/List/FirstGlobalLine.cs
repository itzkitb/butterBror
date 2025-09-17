using bb.Core.Bot;
using bb.Core.Bot.SQLColumnNames;
using bb.Models;
using bb.Utils;
using System.Globalization;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class FirstGlobalLine : CommandBase
    {
        public override string Name => "FirstGlobalLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/FirstGlobalLine.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            {"ru-RU", "Ваше первое сообщение на текущей платформе." },
            {"en-US", "Your first message on the current platform." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=fgl";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["fgl", "firstgloballine", "пргс", "первоеглобальноесообщение"];
        public override string HelpArguments => "(name)";
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
                string name, userID;

                if (data.Arguments.Count > 0)
                {
                    name = TextSanitizer.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    userID = UsernameResolver.GetUserID(name, data.Platform);
                }
                else
                {
                    name = data.User.Name;
                    userID = data.User.ID;
                }

                if (userID == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(name)));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
                else
                {
                    if (name == bb.Bot.Name.ToLower())
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:first_global_line:bot", data.ChannelId, data.Platform));
                    }
                    else
                    {
                        var firstLine = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.FirstMessage);
                        var firstChannel = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.FirstChannel);
                        var firstLineDate = DateTime.Parse((string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.FirstSeen), null, DateTimeStyles.AdjustToUniversal);

                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:first_global_line:user",
                            data.ChannelId,
                            data.Platform,
                            UsernameResolver.Unmention(UsernameResolver.GetUsername(userID, data.Platform, true)),
                            firstLine,
                            TextSanitizer.FormatTimeSpan(Utils.DataConversion.GetTimeTo(firstLineDate, DateTime.UtcNow, false), data.User.Language),
                            firstChannel));
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
