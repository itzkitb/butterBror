using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class Username : CommandBase
    {
        public override string Name => "Name";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Name.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru", "Получить имя из ID." },
            { "en", "Get name from ID." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=name";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["name", "nick", "nickname", "никнейм", "ник", "имя"];
        public override string HelpArguments => "[user ID]";
        public override DateTime CreationDate => DateTime.Parse("25/10/2024");
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
                    string name = Names.GetUsername(data.Arguments[0], PlatformsEnum.Twitch, true);
                    if (name == data.UserID)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:name", data.ChannelID, data.Platform).Replace("%name%", data.UserID)); // Fix AB3
                    }
                    else if (name == null)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform).Replace("%user%", data.Arguments[0])); // Fix AB3
                        commandReturn.SetColor(ChatColorPresets.CadetBlue);
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:name:user", data.ChannelID, data.Platform).Replace("%name%", name).Replace("%id%", data.Arguments[0])); // Fix AB3
                    }
                }
                else
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:name", data.ChannelID, data.Platform).Replace("%name%", data.UserID)); // Fix AB3
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
