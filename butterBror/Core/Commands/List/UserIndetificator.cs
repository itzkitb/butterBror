using butterBror.Utils;
using butterBror.Models;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
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
            { "ru", "Узнать ID пользователя." },
            { "en", "Find out user ID." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=id";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["id", "indetificator", "ид"];
        public override string HelpArguments => "(name)";
        public override DateTime CreationDate => DateTime.Parse("08/08/2024");
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
                if (data.Arguments.Count > 0)
                {
                    string username = Text.UsernameFilter(data.Arguments[0].ToLower());
                    string ID = Names.GetUserID(username, data.Platform, true);
                    if (ID == data.UserID)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:id", data.ChannelID, data.Platform).Replace("%id%", data.UserID));
                    }
                    else if (ID == null)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform).Replace("%user%", username));
                        commandReturn.SetColor(ChatColorPresets.CadetBlue);
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:id:user", data.ChannelID, data.Platform).Replace("%id%", ID).Replace("%user%", Names.DontPing(username)));
                    }
                }
                else
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:id", data.ChannelID, data.Platform).Replace("%id%", data.UserID));
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
