using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using System.Globalization;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List.ChatLines
{
    public class LastGlobalLine : CommandBase
    {
        public override string Name => "LastGlobalLine";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "ChatLines/LastGlobalLine.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Последнее сообщение определенного пользователя." },
            { Language.EnUs, "The last message of the selected user." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"];
        public override string Help => "[name]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (Program.BotInstance.UsersBuffer == null || Program.BotInstance.TwitchName == null || data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                if (data.Arguments != null && data.Arguments.Count != 0)
                {
                    string name = TextSanitizer.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                    string userID = UsernameResolver.GetUserID(name, Platform.Twitch, true);

                    if (userID == null || Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.LastSeen) == null)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(name)));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    else
                    {
                        string lastLine = (string)Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.LastMessage);
                        string lastChannel = (string)Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.LastChannel);
                        DateTime lastLineDate = DateTime.Parse((string)Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(userID), Users.LastSeen), null, DateTimeStyles.AdjustToUniversal);

                        if (name == Program.BotInstance.TwitchName.ToLower())
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:last_global_line:bot", data.ChannelId, data.Platform));
                        }
                        else if (name == data.User.Name.ToLower())
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "text:you_right_there", data.ChannelId, data.Platform));
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:last_global_line",
                                data.ChannelId,
                                data.Platform,
                                UsernameResolver.Unmention(UsernameResolver.GetUsername(userID, data.Platform, true)),
                                lastLine,
                                TextSanitizer.FormatTimeSpan(DataConversion.GetTimeTo(lastLineDate, DateTime.UtcNow, false), data.User.Language),
                                lastChannel));
                        }
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "text:you_right_there", data.ChannelId, data.Platform));
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
