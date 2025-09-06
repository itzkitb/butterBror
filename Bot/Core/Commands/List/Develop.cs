using bb.Core.Bot;
using bb.Models;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class Dev : CommandBase
    {
        public override string Name => "Dev";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Dev.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Эта команда не для тебя PauseChamp" },
            { "en-US", "This command is not for you PauseChamp" }
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
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                DateTime StartTime = DateTime.Now;

                try
                {
                    string result = CodeExecutor.Run(data.ArgumentsString);
                    DateTime EndTime = DateTime.Now;
                    string message = LocalizationService.GetString(data.User.Language, "command:csharp:result", data.ChannelId, data.Platform, result, (int)(EndTime - StartTime).TotalMilliseconds);
                    if (message == "command:csharp:result")
                    {
                        message = $"TE:{result} ({(int)(EndTime - StartTime).TotalMilliseconds}ms)";
                    }
                    commandReturn.SetMessage(message);

                }
                catch (Exception ex)
                {
                    DateTime EndTime = DateTime.Now;
                    string message = LocalizationService.GetString(data.User.Language, "command:csharp:error", data.ChannelId, data.Platform, ex.Message, (int)(EndTime - StartTime).TotalMilliseconds);
                    if (message == "command:csharp:error")
                    {
                        message = $"TE:{ex.Message} ({(int)(EndTime - StartTime).TotalMilliseconds}ms)";
                    }
                    commandReturn.SetMessage(message);

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