using butterBror.Models;
using Discord.WebSocket;
using Telegram.Bot.Types;
using TwitchLib.Client.Events;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Commands
{
    public class Executor
    {
        /// <summary>
        /// Handles command execution for Twitch platform through chat commands.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="command">Twitch chat command event arguments.</param>
        /// <remarks>
        /// - Processes both standard and reply commands
        /// - Extracts command name and arguments
        /// - Builds user data with Twitch-specific properties
        /// - Routes command to execution pipeline
        /// - Handles empty command cases gracefully
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "Twitch")]
        public static async void Twitch(object sender, OnChatCommandReceivedArgs command)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                string CommandName = command.Command.CommandText;
                List<string> CommandArguments = command.Command.ArgumentsAsList;
                string CommandArgumentsAsString = command.Command.ArgumentsAsString;

                if (CommandName == string.Empty && CommandArguments.Count > 0)
                {
                    CommandName = CommandArguments[0];
                    CommandArguments = CommandArguments.Skip(1).ToList();
                    CommandArgumentsAsString = string.Join(" ", CommandArguments);
                }
                else if (CommandName == string.Empty && CommandArguments.Count == 0)
                {
                    return;
                }

                UserData user = new()
                {
                    ID = command.Command.ChatMessage.UserId,
                    Language = "en",
                    Name = command.Command.ChatMessage.Username,
                    IsModerator = command.Command.ChatMessage.IsModerator,
                    IsBroadcaster = command.Command.ChatMessage.IsBroadcaster
                };

                CommandData data = new()
                {
                    Name = CommandName.ToLower(),
                    UserID = command.Command.ChatMessage.UserId,
                    Arguments = CommandArguments,
                    ArgumentsString = CommandArgumentsAsString,
                    Channel = command.Command.ChatMessage.Channel,
                    ChannelID = command.Command.ChatMessage.RoomId,
                    MessageID = command.Command.ChatMessage.Id,
                    Platform = PlatformsEnum.Twitch,
                    User = user,
                    TwitchArguments = command,
                    CommandInstanceID = Guid.NewGuid().ToString()
                };

                if (command.Command.ChatMessage.ChatReply != null)
                {
                    string[] trimedReplyText = command.Command.ChatMessage.ChatReply.ParentMsgBody.Split(' ');
                    data.Arguments.AddRange(trimedReplyText);
                    data.ArgumentsString = data.ArgumentsString + command.Command.ChatMessage.ChatReply.ParentMsgBody;
                }
                await Runner.Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Processes Discord slash commands with structured arguments.
        /// </summary>
        /// <param name="command">Discord slash command interaction data.</param>
        /// <remarks>
        /// - Converts slash command options to flat arguments
        /// - Builds comprehensive command context including server info
        /// - Uses GUID for request tracing
        /// - Maintains support for slash command responses
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "Discord#1")]
        public static async void Discord(SocketSlashCommand command)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                UserData user = new()
                {
                    ID = command.User.Id.ToString(),
                    Language = "en",
                    Name = command.User.Username
                };
                Guid RequestUuid = Guid.NewGuid();
                Guid CommandExecutionUuid = Guid.NewGuid();
                string RequestUuidString = RequestUuid.ToString();
                string ArgsAsString = "";
                Dictionary<string, dynamic> argsDS = new();
                List<string> args = new();
                foreach (var info in command.Data.Options)
                {
                    ArgsAsString += info.Value.ToString();
                    argsDS.Add(info.Name, info.Value);
                    args.Add(info.Value.ToString());
                }

                CommandData data = new()
                {
                    Name = command.CommandName.ToLower(),
                    UserID = command.User.Id.ToString(),
                    DiscordArguments = argsDS,
                    Channel = command.Channel.Name,
                    ChannelID = command.Channel.Id.ToString(),
                    Server = ((SocketGuildChannel)command.Channel).Guild.Name,
                    ServerID = ((SocketGuildChannel)command.Channel).Guild.Id.ToString(),
                    Platform = PlatformsEnum.Discord,
                    DiscordCommandBase = command,
                    User = user,
                    ArgumentsString = ArgsAsString,
                    Arguments = args,
                    CommandInstanceID = CommandExecutionUuid.ToString()
                };
                await Runner.Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Handles Discord text-based commands from message content.
        /// </summary>
        /// <param name="message">Discord message containing the command.</param>
        /// <remarks>
        /// - Parses message content into command structure
        /// - Handles both direct messages and guild channels
        /// - Builds command context with user/permission data
        /// - Routes processed command to execution pipeline
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "Discord#2")]
        public static async void Discord(SocketMessage message)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                string CommandName = message.Content.Split(' ')[0].Remove(0, 1);
                List<string> CommandArguments = message.Content.Split(' ').Skip(1).ToList();
                string CommandArgumentsAsString = string.Join(' ', CommandArguments);

                if (CommandName == string.Empty && CommandArguments.Count > 0)
                {
                    CommandName = CommandArguments[0];
                    CommandArguments = CommandArguments.Skip(1).ToList();
                    CommandArgumentsAsString = string.Join(" ", CommandArguments);
                }
                else if (CommandName == string.Empty && CommandArguments.Count == 0)
                {
                    return;
                }

                UserData user = new()
                {
                    ID = message.Author.Id.ToString(),
                    Language = "en",
                    Name = message.Author.Username
                };

                CommandData data = new()
                {
                    Name = CommandName,
                    UserID = message.Author.Id.ToString(),
                    Channel = message.Channel.Name,
                    ChannelID = message.Channel.Id.ToString(),
                    Server = ((SocketGuildChannel)message.Channel).Guild.Name,
                    ServerID = ((SocketGuildChannel)message.Channel).Guild.Id.ToString(),
                    Platform = PlatformsEnum.Discord,
                    User = user,
                    ArgumentsString = CommandArgumentsAsString,
                    Arguments = CommandArguments,
                    CommandInstanceID = Guid.NewGuid().ToString()
                };
                await Runner.Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Processes Telegram commands from text messages including replies.
        /// </summary>
        /// <param name="message">Telegram message containing the command.</param>
        /// <remarks>
        /// - Extracts command name and arguments from text
        /// - Handles reply message context
        /// - Builds user data with Telegram-specific properties
        /// - Supports both public and private chat contexts
        /// - Routes command to execution pipeline
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "Telegram")]
        public static async void Telegram(Message message)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                UserData user = new()
                {
                    ID = message.From.Id.ToString(),
                    Language = "en",
                    Name = message.From.Username ?? message.From.FirstName,
                    IsModerator = false,
                    IsBroadcaster = message.From.Id == message.Chat.Id
                };

                Guid command_execution_uid = Guid.NewGuid();
                CommandData data = new()
                {
                    Name = message.Text.ToLower().Split(' ')[0],
                    UserID = message.From.Id.ToString(),
                    Arguments = message.Text.Split(' ').Skip(1).ToList(),
                    ArgumentsString = string.Join(" ", message.Text.Split(' ').Skip(1)),
                    Channel = message.Chat.Title ?? message.Chat.Username ?? message.Chat.Id.ToString(),
                    ChannelID = message.Chat.Id.ToString(),
                    Platform = PlatformsEnum.Telegram,
                    User = user,
                    CommandInstanceID = command_execution_uid.ToString(),
                    TelegramMessage = message,
                    MessageID = message.Id.ToString()
                };

                if (message.ReplyToMessage != null)
                {
                    string[] trimmedReplyText = message.ReplyToMessage.Text.Split(' ');
                    data.Arguments.AddRange(trimmedReplyText);
                    data.ArgumentsString += " " + string.Join(" ", trimmedReplyText);
                }

                await Runner.Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
