using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List.Development
{
    public class BbScript : CommandBase
    {
        public override string Name => "bbScript";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Development/BbScript.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Эта команда не для тебя PauseChamp" },
            { Language.EnUs, "This command is not for you PauseChamp" }
        };
        public override int UserCooldown => 0;
        public override int Cooldown => 0;
        public override string[] Aliases => ["bbrun", "bbcode", "bbscript", "bbdev"];
        public override string Help => "<code>";
        public override DateTime CreationDate => DateTime.Parse("2025-09-07T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.BotMod;
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

                DateTime StartTime = DateTime.Now;

                try
                {
                    Script interpretator = new Script();
                    string result = (string)(interpretator.Execute(data.ArgumentsString) ?? "null");
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