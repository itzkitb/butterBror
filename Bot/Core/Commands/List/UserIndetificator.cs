using bb.Core.Bot;
using bb.Models;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class UserIndetificator : CommandBase
    {
        public override string Name => "ID";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/UserIndetificator.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Узнать ID пользователя." },
            { "en-US", "Find out user ID." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=id";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["id", "indetificator", "ид"];
        public override string HelpArguments => "(name)";
        public override DateTime CreationDate => DateTime.Parse("2024-08-08T00:00:00.0000000Z");
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
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                if (data.Arguments != null && data.Arguments.Count > 0)
                {
                    string username = TextSanitizer.UsernameFilter(data.Arguments[0].ToLower());
                    string ID = UsernameResolver.GetUserID(username, data.Platform, true);
                    if (ID == data.User.Id)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:id", data.ChannelId, data.Platform, data.User.Id));
                    }
                    else if (ID == null)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, username));
                        commandReturn.SetColor(ChatColorPresets.CadetBlue);
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:id:user", data.ChannelId, data.Platform, UsernameResolver.Unmention(username), ID));
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:id", data.ChannelId, data.Platform, data.User.Id));
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
