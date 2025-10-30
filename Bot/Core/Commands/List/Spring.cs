using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;

namespace bb.Core.Commands.List
{
    public class Spring : CommandBase
    {
        public override string Name => "Spring";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Spring.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Узнать, сколько времени осталось до начала/конца весны." },
            { Language.EnUs, "Find out how much time is left until the beginning/end of spring." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=spring";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["spring", "sp", "весна"];
        public override string HelpArguments => "(name)";
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

                commandReturn.SetMessage(TextSanitizer.TimeTo(
                    new(2000, 3, 1),
                    new(2000, 6, 1),
                    "spring",
                    data.User.Language,
                    data.ArgumentsString,
                    data.ChannelId,
                    data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
