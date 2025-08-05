using butterBror.Utils;
using butterBror.Data;
using butterBror.Models;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Commands.List
{
    public class Tuck : CommandBase
    {
        public override string Name => "Tuck";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Tuck.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Спокойной ночи... 👁" },
            { "en-US", "Good night... 👁" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=tuck";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["tuck", "уложить", "tk", "улож", "тык"];
        public override string HelpArguments => "(name) (text)";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        
        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                commandReturn.SetColor(ChatColorPresets.HotPink);
                if (data.Arguments.Count >= 1)
                {
                    var username = Text.UsernameFilter(Text.CleanAsciiWithoutSpaces(data.Arguments[0]));
                    var isSelectedUserIsNotIgnored = true;
                    var userID = Names.GetUserID(username.ToLower(), PlatformsEnum.Twitch);
                    try
                    {
                        if (userID != null)
                            isSelectedUserIsNotIgnored = !(Engine.Bot.SQL.Roles.GetIgnoredUser(data.Platform, Format.ToLong(data.User.ID)) is not null);
                    }
                    catch (Exception) { }
                    if (username.ToLower() == Engine.Bot.BotName.ToLower())
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
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:tuck:text", data.ChannelId, data.Platform, Names.DontPing(username), string.Join(" ", list)));
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:tuck", data.ChannelId, data.Platform, Names.DontPing(username)));
                        }
                    }
                    else
                    {
                        Write($"User @{data.User.Name} tried to put a user to sleep who is in the ignore list", "info");
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
