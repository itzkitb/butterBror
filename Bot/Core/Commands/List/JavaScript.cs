using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using Jint;
using TwitchLib.Client.Enums;
using static bb.Core.Bot.Logger;

namespace bb.Core.Commands.List
{
    public class JavaScript : CommandBase
    {
        public override string Name => "JavaScript";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/JavaScript.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Выполнить JavaScript код в V8 и получить ответ." },
            { Language.EnUs, "Execute JavaScript code in V8 and get the response." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=jaba";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["js", "javascript", "джаваскрипт", "жс", "jabascript", "supinic"];
        public override string HelpArguments => "[code]";
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
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

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
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
