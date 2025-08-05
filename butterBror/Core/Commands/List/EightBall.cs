using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class Eightball : CommandBase
    {
        public override string Name => "EightBall";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Eightball.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Этот шар умеет отвечать на вопросы." },
            { "en-US", "This ball can answer questions." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=8ball";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["8ball", "eightball", "eb", "8b", "шар"];
        public override string HelpArguments => string.Empty;
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
                int stage1 = new Random().Next(1, 5);
                int stage2 = new Random().Next(1, 6);
                string translationParam = "command:8ball:";
                if (stage1 == 1)
                {
                    commandReturn.SetColor(ChatColorPresets.DodgerBlue);
                    translationParam += "positively:" + stage2;
                }
                else if (stage1 == 2)
                {
                    translationParam += "hesitantly:" + stage2;
                }
                else if (stage1 == 3)
                {
                    commandReturn.SetColor(ChatColorPresets.GoldenRod);
                    translationParam += "neutral:" + stage2;
                }
                else if (stage1 == 4)
                {
                    commandReturn.SetColor(ChatColorPresets.Red);
                    translationParam += "negatively:" + stage2;
                }
                commandReturn.SetMessage("🔮 " + LocalizationService.GetString(data.User.Language, translationParam, data.ChannelId, data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}