using butterBror.Utils;
using butterBror.Data;
using butterBror.Models;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class LastGlobalLine : CommandBase
    {
        public override string Name => "LastGlobalLine";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/LastGlobalLine.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Последнее сообщение определенного пользователя." },
            { "en", "The last message of the selected user." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=lgl";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"];
        public override string HelpArguments => "[name]";
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
                if (data.Arguments.Count != 0)
                {
                    var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    var userID = Names.GetUserID(name, PlatformsEnum.Twitch);
                    if (userID == null)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                            .Replace("%user%", Names.DontPing(name)));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    else
                    {
                        var lastLine = UsersData.Get<string>(userID, "lastSeenMessage", data.Platform);
                        var lastLineDate = UsersData.Get<DateTime>(userID, "lastSeen", data.Platform);
                        DateTime now = DateTime.UtcNow;
                        if (name == Engine.Bot.BotName.ToLower())
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_global_line:bot", data.ChannelID, data.Platform));
                        }
                        else if (name == data.User.Name.ToLower())
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_global_line", data.ChannelID, data.Platform)
                                .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.Platform)))
                                .Replace("&timeAgo&", Text.FormatTimeSpan(Utils.Format.GetTimeTo(lastLineDate, now, false), data.User.Language))
                                .Replace("%message%", lastLine));
                        }
                    }
                }
                else
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
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
