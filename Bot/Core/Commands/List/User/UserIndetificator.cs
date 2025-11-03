using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List.User
{
    public class UserIndetificator : CommandBase
    {
        public override string Name => "ID";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "User/UserIndetificator.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Узнать ID пользователя." },
            { Language.EnUs, "Find out user ID." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["id", "indetificator", "ид"];
        public override string Help => "<username>";
        public override DateTime CreationDate => DateTime.Parse("2024-08-08T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
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

                if (data.Arguments != null && data.Arguments.Count > 0)
                {
                    string username = TextSanitizer.UsernameFilter(data.Arguments[0].ToLower());
                    string ID = UsernameResolver.GetUserID(username, data.Platform, true);
                    if (ID == data.User.Id)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:id", data.ChannelId, data.Platform, data.User.Id));
                    }
                    else if (ID == null)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, username));
                        commandReturn.SetColor(ChatColorPresets.CadetBlue);
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:id:user", data.ChannelId, data.Platform, UsernameResolver.Unmention(username), ID));
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:id", data.ChannelId, data.Platform, data.User.Id));
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
