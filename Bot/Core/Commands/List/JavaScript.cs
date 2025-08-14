using butterBror.Core.Bot;
using butterBror.Models;
using butterBror.Utils;
using Jint;
using TwitchLib.Client.Enums;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Commands.List
{
    public class JavaScript : CommandBase
    {
        public override string Name => "Java";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/JavaScript.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Выполнить JavaScript код в V8 и получить ответ." },
            { "en-US", "Execute JavaScript code in V8 and get the response." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=jaba";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["js", "javascript", "джаваскрипт", "жс", "jabascript", "supinic"];
        public override string HelpArguments => "[code]";
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
                if (new BannedWordDetector().Check(data.ArgumentsString, data.ChannelId, data.Platform))
                {
                    try
                    {
                        var engine = new Jint.Engine(cfg => cfg
        .LimitRecursion(100)
        .LimitMemory(40 * 1024 * 1024)
        .Strict()
        .LocalTimeZone(TimeZoneInfo.Utc));
                        var isSafe = true;
                        engine.SetValue("navigator", new Action(() => isSafe = false));
                        engine.SetValue("WebSocket", new Action(() => isSafe = false));
                        engine.SetValue("XMLHttpRequest", new Action(() => isSafe = false));
                        engine.SetValue("fetch", new Action(() => isSafe = false));
                        string jsCode = data.ArgumentsString;
                        var result = engine.Evaluate(jsCode);

                        if (isSafe)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:js", data.ChannelId, data.Platform, result.ToString()));
                        }
                        else
                        {
                            commandReturn.SetColor(ChatColorPresets.OrangeRed);
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:js", data.ChannelId, data.Platform, "Not allowed"));
                        }
                    }
                    catch (Exception ex)
                    {
                        commandReturn.SetColor(ChatColorPresets.Firebrick);
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:js", data.ChannelId, data.Platform, ex.Message));
                        Write(ex);
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
