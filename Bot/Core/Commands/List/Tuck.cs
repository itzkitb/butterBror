using bb.Utils;
using bb.Core.Configuration;
using TwitchLib.Client.Enums;
using static bb.Core.Bot.Console;
using bb.Models.Command;
using bb.Models.Platform;

namespace bb.Core.Commands.List
{
    public class Tuck : CommandBase
    {
        public override string Name => "Tuck";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Tuck.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Спокойной ночи... 👁" },
            { "en-US", "Good night... 👁" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=tuck";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["tuck", "уложить", "tk", "улож", "тык"];
        public override string HelpArguments => "(name) (text)";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
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
                if (data.ChannelId == null || bb.Program.BotInstance.DataBase == null || bb.Program.BotInstance.TwitchName == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                commandReturn.SetColor(ChatColorPresets.HotPink);
                if (data.Arguments != null && data.Arguments.Count >= 1)
                {
                    var username = TextSanitizer.UsernameFilter(TextSanitizer.CleanAsciiWithoutSpaces(data.Arguments[0]));
                    var isSelectedUserIsNotIgnored = true;
                    var userID = UsernameResolver.GetUserID(username.ToLower(), PlatformsEnum.Twitch);
                    try
                    {
                        if (userID != null)
                            isSelectedUserIsNotIgnored = !(bb.Program.BotInstance.DataBase.Roles.GetIgnoredUser(data.Platform, DataConversion.ToLong(data.User.Id)) is not null);
                    }
                    catch (Exception) { }
                    if (username.ToLower() == bb.Program.BotInstance.TwitchName.ToLower())
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:tuck:bot", data.ChannelId, data.Platform));
                        commandReturn.SetColor(ChatColorPresets.CadetBlue);
                    }
                    else if (isSelectedUserIsNotIgnored)
                    {
                        if (data.Arguments.Count >= 2)
                        {
                            List<string> list = data.Arguments;
                            list.RemoveAt(0);
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:tuck:text", data.ChannelId, data.Platform, UsernameResolver.Unmention(username), string.Join(" ", list)));
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:tuck", data.ChannelId, data.Platform, UsernameResolver.Unmention(username)));
                        }
                    }
                    else
                    {
                        Write($"User @{data.User.Name} tried to put a user to sleep who is in the ignore list.");
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_ignored", data.ChannelId, data.Platform));
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:tuck:none", data.ChannelId, data.Platform));
                    commandReturn.SetColor(ChatColorPresets.CadetBlue);
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
