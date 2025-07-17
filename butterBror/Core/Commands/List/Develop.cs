using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class Dev : CommandBase
    {
        public override string Name => "Dev";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Dev.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru", "Эта команда не для тебя PauseChamp" },
            { "en", "This command is not for you PauseChamp" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=dev";
        public override int CooldownPerUser => 0;
        public override int CooldownPerChannel => 0;
        public override string[] Aliases => ["run", "code", "csharp", "dev"];
        public override string HelpArguments => "[crap code]";
        public override DateTime CreationDate => DateTime.Parse("26/09/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => true;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                DateTime StartTime = DateTime.Now;

                try
                {
                    string result = Command.ExecuteCode(data.ArgumentsString);
                    DateTime EndTime = DateTime.Now;
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:csharp:result", data.ChannelID, data.Platform)
                        .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                        .Replace("%result%", result));
                }
                catch (Exception ex)
                {
                    DateTime EndTime = DateTime.Now;
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:csharp:error", data.ChannelID, data.Platform)
                        .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                        .Replace("%result%", ex.Message));
                    commandReturn.SetColor(ChatColorPresets.Red);
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