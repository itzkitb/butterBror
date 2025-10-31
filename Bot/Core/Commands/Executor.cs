using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using Discord.WebSocket;
using System;
using Telegram.Bot.Types;
using TwitchLib.Client.Events;
using static bb.Core.Bot.Logger;

namespace bb.Core.Commands
{
    public class Executor
    {
        /// <summary>
        /// Processes and executes Twitch chat commands from channel messages.
        /// </summary>
        /// <param name="sender">Event source object (typically TwitchClient instance)</param>
        /// <param name="message">Event arguments containing command details and message context</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Handles both standard commands and reply-based commands through ChatReply property</item>
        /// <item>Normalizes command structure by extracting command name and arguments</item>
        /// <item>Builds comprehensive user context with Twitch-specific properties:
        /// <list type="table">
        /// <item><term>IsModerator</term><description>Indicates moderator status in channel</description></item>
        /// <item><term>IsBroadcaster</term><description>Verifies if user is channel owner</description></item>
        /// </list>
        /// </item>
        /// <item>Generates unique command instance ID (GUID) for request tracing</item>
        /// <item>Processes reply context by appending parent message content to arguments</item>
        /// <item>Gracefully skips empty command cases without throwing exceptions</item>
        /// <item>Forwards command data to Runner pipeline for execution</item>
        /// </list>
        /// All exceptions are logged through the Console.Write system but don't interrupt bot operation.
        /// </remarks>
        public async void Twitch(object? sender, OnMessageReceivedArgs message)
        {
            try
            {
                var cMessage = message.ChatMessage;

                if (bb.Program.BotInstance.DataBase == null)
                {
                    Write("The database is null.", LogLevel.Critical);
                    return;
                }

                if (!cMessage.Message.StartsWith(bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Twitch, cMessage.RoomId)))
                {
                    return;
                }

                string trimmedMessage = cMessage.Message.Replace("\u2063", "").Substring(bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Twitch, cMessage.RoomId).Length);
                string[] parts = trimmedMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string commandName = parts.Length > 0 ? parts[0] : string.Empty;
                List<string> commandArguments = parts.Length > 1 ? parts.Skip(1).ToList() : new List<string>();
                string commandArgumentsAsString = string.Join(" ", commandArguments);

                if (string.IsNullOrEmpty(commandName) && commandArguments.Count > 0)
                {
                    commandName = commandArguments[0];
                    commandArguments = commandArguments.Skip(1).ToList();
                    commandArgumentsAsString = string.Join(" ", commandArguments);
                }
                else if (string.IsNullOrEmpty(commandName) && commandArguments.Count == 0)
                {
                    return;
                }

                UserData user = new()
                {
                    Id = cMessage.UserId,
                    Language = (Language)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Twitch, DataConversion.ToLong(cMessage.UserId), Configuration.Users.Language)),
                    Name = cMessage.Username,
                    Balance = bb.Program.BotInstance.Currency.GetBalance(cMessage.UserId, Platform.Twitch),
                    Roles = (Roles)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Twitch, DataConversion.ToLong(cMessage.UserId), Configuration.Users.Role)),
                };

                CommandData data = new()
                {
                    Name = commandName.ToLower(),
                    Arguments = commandArguments,
                    ArgumentsString = commandArgumentsAsString,
                    Channel = cMessage.Channel,
                    ChannelId = cMessage.RoomId,
                    MessageID = cMessage.Id,
                    Platform = Platform.Twitch,
                    User = user,
                    TwitchMessage = message,
                    CommandInstanceID = Guid.NewGuid().ToString(),
                    ChatID = cMessage.RoomId
                };

                if (cMessage.ChatReply != null)
                {
                    string[] replyParts = cMessage.ChatReply.ParentMsgBody.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    data.Arguments.AddRange(replyParts);
                    data.ArgumentsString += " " + cMessage.ChatReply.ParentMsgBody;
                }

                await bb.Program.BotInstance.CommandRunner.Execute(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Processes Discord slash commands with structured parameter handling.
        /// </summary>
        /// <param name="command">Interaction object containing slash command data and context</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Converts Discord's hierarchical command options into flat argument structure</item>
        /// <item>Creates dual GUID system for request tracking:
        /// <list type="table">
        /// <item><term>RequestUuid</term><description>Tracks the original interaction request</description></item>
        /// <item><term>CommandExecutionUuid</term><description>Identifies specific command execution instance</description></item>
        /// </list>
        /// </item>
        /// <item>Builds complete context including:
        /// <list type="bullet">
        /// <item>Channel name and ID</item>
        /// <item>Guild/server name and ID</item>
        /// <item>User identification and metadata</item>
        /// <item>Command-specific options dictionary</item>
        /// </list>
        /// </item>
        /// <item>Maintains reference to original interaction for response handling</item>
        /// <item>Preserves command structure for proper slash command response patterns</item>
        /// <item>Handles both guild and direct message contexts transparently</item>
        /// </list>
        /// Automatically processes all command options regardless of nesting depth.
        /// </remarks>
        public async void Discord(SocketSlashCommand command)
        {
            try
            {
                UserData user = new()
                {
                    Id = command.User.Id.ToString(),
                    Language = (Language)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Discord, Convert.ToInt64(command.User.Id), Configuration.Users.Language)),
                    Name = command.User.Username,
                    Balance = bb.Program.BotInstance.Currency.GetBalance(command.User.Id.ToString(), Platform.Discord),
                    Roles = (Roles)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Discord, Convert.ToInt64(command.User.Id), Configuration.Users.Role)),
                };
                
                Guid RequestUuid = Guid.NewGuid();
                Guid CommandExecutionUuid = Guid.NewGuid();
                string RequestUuidString = RequestUuid.ToString();
                string ArgsAsString = "";
                List<string> args = [];
                foreach (var info in command.Data.Options)
                {
                    ArgsAsString += info.Value.ToString();
                    args.Add(info.Value.ToString() ?? "");
                }

                CommandData data = new()
                {
                    Name = command.CommandName.ToLower(),
                    Channel = command.Channel.Name,
                    ChannelId = command.Channel.Id.ToString(),
                    Server = ((SocketGuildChannel)command.Channel).Guild.Name,
                    ServerID = ((SocketGuildChannel)command.Channel).Guild.Id.ToString(),
                    Platform = Platform.Discord,
                    DiscordCommandBase = command,
                    User = user,
                    ArgumentsString = ArgsAsString,
                    Arguments = args,
                    CommandInstanceID = CommandExecutionUuid.ToString(),
                    ChatID = ((SocketGuildChannel)command.Channel).Guild.Id.ToString()
                };
                await bb.Program.BotInstance.CommandRunner.Execute(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Handles legacy text-based commands in Discord channels and direct messages.
        /// </summary>
        /// <param name="message">Message object containing command text and context</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Processes commands prefixed with single character</item>
        /// <item>Normalizes command structure by removing prefix and splitting arguments</item>
        /// <item>Distinguishes between guild channels and direct messages:
        /// <list type="bullet">
        /// <item>Guild channels: Includes server/guild context</item>
        /// <item>Direct messages: Server fields remain null</item>
        /// </list>
        /// </item>
        /// <item>Handles edge cases where command name might be empty</item>
        /// <item>Generates unique command instance ID (GUID) for execution tracking</item>
        /// <item>Builds minimal user context with essential identification data</item>
        /// <item>Forwards processed command to execution pipeline for processing</item>
        /// </list>
        /// Command format: [prefix][command] [arguments] (e.g., "!help moderation")
        /// Throws are caught and logged without disrupting message processing flow.
        /// </remarks>
        public async void Discord(SocketMessage message)
        {
            try
            {
                if (bb.Program.BotInstance.DataBase == null)
                {
                    Write("The database is null.", LogLevel.Critical);
                    return;
                }

                if (!message.Content.StartsWith(bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Discord, ((SocketGuildChannel)message.Channel).Guild.Id.ToString())))
                {
                    return;
                }

                string CommandName = message.Content.Split(' ')[0].Substring(bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Discord, ((SocketGuildChannel)message.Channel).Guild.Id.ToString()).Length);
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
                    Id = message.Author.Id.ToString(),
                    Language = (Language)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Discord, Convert.ToInt64(message.Author.Id.ToString()), Configuration.Users.Language)),
                    Name = message.Author.Username,
                    Balance = bb.Program.BotInstance.Currency.GetBalance(message.Author.Id.ToString(), Platform.Discord),
                    Roles = (Roles)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Discord, Convert.ToInt64(message.Author.Id.ToString()), Configuration.Users.Role)),
                };

                CommandData data = new()
                {
                    Name = CommandName,
                    Channel = message.Channel.Name,
                    ChannelId = message.Channel.Id.ToString(),
                    Server = ((SocketGuildChannel)message.Channel).Guild.Name,
                    ServerID = ((SocketGuildChannel)message.Channel).Guild.Id.ToString(),
                    Platform = Platform.Discord,
                    User = user,
                    ArgumentsString = CommandArgumentsAsString,
                    Arguments = CommandArguments,
                    CommandInstanceID = Guid.NewGuid().ToString(),
                    ChatID = ((SocketGuildChannel)message.Channel).Guild.Id.ToString(),
                    DiscordMessage = message
                };
                await bb.Program.BotInstance.CommandRunner.Execute(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Processes Telegram bot commands from text messages and replies.
        /// </summary>
        /// <param name="message">Incoming message object containing command data</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Builds comprehensive user context with Telegram-specific properties:
        /// <list type="table">
        /// <item><term>IsBroadcaster</term><description>True when user ID matches chat ID (private chat)</description></item>
        /// <item><term>Name</term><description>Uses username if available, falls back to first name</description></item>
        /// </list>
        /// </item>
        /// <item>Processes reply context by appending replied message content to arguments</item>
        /// <item>Handles both public group and private chat contexts seamlessly</item>
        /// <item>Generates unique command instance ID (GUID) for execution tracking</item>
        /// <item>Maintains reference to original message for response operations</item>
        /// </list>
        /// Command structure: First word is command name, remaining words are arguments.
        /// All exceptions are logged through the Console.Write system but don't interrupt processing.
        /// </remarks>
        public async void Telegram(Message message)
        {
            try
            {
                if (bb.Program.BotInstance.DataBase == null)
                {
                    Write("The database is null.", LogLevel.Critical);
                    return;
                }

                if (message.From == null)
                {
                    Write("Unknown Telegram user (API error?).", LogLevel.Error);
                    return;
                }

                if (message.Text == null || !message.Text.StartsWith(bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Telegram, message.Chat.Id.ToString())))
                {
                    return;
                }

                UserData user = new()
                {
                    Id = message.From.Id.ToString(),
                    Language = (Language)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Telegram, Convert.ToInt64(message.From.Id.ToString()), Configuration.Users.Language)),
                    Name = message.From.Username ?? message.From.FirstName,
                    Balance = bb.Program.BotInstance.Currency.GetBalance(message.From.Id.ToString(), Platform.Telegram),
                    Roles = (Roles)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(Platform.Telegram, Convert.ToInt64(message.From.Id.ToString()), Configuration.Users.Role)),
                };

                Guid command_execution_uid = Guid.NewGuid();
                CommandData data = new()
                {
                    Name = message.Text.ToLower().Split(' ')[0].Substring(bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Telegram, message.Chat.Id.ToString()).Length),
                    Arguments = message.Text.Split(' ').Skip(1).ToList(),
                    ArgumentsString = string.Join(" ", message.Text.Split(' ').Skip(1)),
                    Channel = message.Chat.Title ?? message.Chat.Username ?? message.Chat.Id.ToString(),
                    ChannelId = message.Chat.Id.ToString(),
                    Platform = Platform.Telegram,
                    User = user,
                    CommandInstanceID = command_execution_uid.ToString(),
                    TelegramMessage = message,
                    MessageID = message.Id.ToString(),
                    ChatID = message.Chat.Id.ToString()
                };

                if (message.ReplyToMessage != null)
                {
                    string[] trimmedReplyText = (message.ReplyToMessage.Text ?? "").Split(' ');
                    data.Arguments.AddRange(trimmedReplyText);
                    data.ArgumentsString += " " + string.Join(" ", trimmedReplyText);
                }

                await bb.Program.BotInstance.CommandRunner.Execute(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
