using butterBror.Utils;
using butterBror.Data;
using butterBror.Models;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class FirstGlobalLine : CommandBase
    {
        public override string Name => "FirstGlobalLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/FirstGlobalLine.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            {"ru", "Ваше первое сообщение на текущей платформе." },
            {"en", "Your first message on the current platform." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=fgl";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["fgl", "firstgloballine", "пргс", "первоеглобальноесообщение"];
        public override string HelpArguments => "(name)";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
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
                DateTime now = DateTime.UtcNow;

                if (data.Arguments.Count != 0)
                {
                    var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    var userID = Names.GetUserID(name, data.Platform);
                    if (userID == null)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                            .Replace("%user%", Names.DontPing(name)));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    else
                    {
                        var firstLine = UsersData.Get<string>(userID, "firstMessage", data.Platform);
                        var firstLineDate = UsersData.Get<DateTime>(userID, "firstSeen", data.Platform);

                        if (name == Engine.Bot.BotName.ToLower())
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line:bot", data.ChannelID, data.Platform));
                        }
                        else if (name == data.User.Name)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line", data.ChannelID, data.Platform)
                                .Replace("%ago%", Text.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.User.Language))
                                .Replace("%message%", firstLine));
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line:user", data.ChannelID, data.Platform)
                                .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.Platform)))
                                .Replace("%ago%", Text.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.User.Language))
                                .Replace("%message%", firstLine));
                        }
                    }
                }
                else
                {
                    var firstLine = UsersData.Get<string>(data.UserID, "firstMessage", data.Platform);
                    var firstLineDate = UsersData.Get<DateTime>(data.UserID, "firstSeen", data.Platform);

                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line", data.ChannelID, data.Platform)
                        .Replace("%ago%", Text.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.User.Language))
                        .Replace("%message%", firstLine));
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
