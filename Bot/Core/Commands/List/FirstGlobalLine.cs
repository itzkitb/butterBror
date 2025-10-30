using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
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
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Ваше первое сообщение на текущей платформе." },
            { Language.EnUs, "Your first message on the current platform." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=fgl";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["fgl", "firstgloballine", "пргс", "первоеглобальноесообщение"];
        public override string HelpArguments => "(name)";
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
                if (bb.Program.BotInstance.UsersBuffer == null || data.ChannelId == null || bb.Program.BotInstance.TwitchName == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                string name, userID;

                if (data.Arguments != null && data.Arguments.Count > 0)
                {
                    name = TextSanitizer.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    userID = UsernameResolver.GetUserID(name, data.Platform);
                }
                else
                {
                    name = data.User.Name;
                    userID = data.User.Id;
                }

                if (userID == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(name)));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
                else
                {
                    if (name == bb.Program.BotInstance.TwitchName.ToLower())
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:first_global_line:bot", data.ChannelId, data.Platform));
                    }
                    else
                    {
                        var firstLine = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.FirstMessage);
                        var firstChannel = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.FirstChannel);
                        var firstLineDate = DateTime.Parse((string)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.FirstSeen), null, DateTimeStyles.AdjustToUniversal);

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
