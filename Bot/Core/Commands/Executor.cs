using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using Discord.WebSocket;
using System;
using Telegram.Bot.Types;
using TwitchLib.Client.Events;
using static bb.Core.Bot.Console;

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
        public static async void Twitch(object? sender, OnMessageReceivedArgs message)
        {
            try
            {
                if (bb.Bot.DataBase == null)
                {
                    Write("The database is null.", LogLevel.Critical);
                    return;
                }

                if (!message.ChatMessage.Message.StartsWith(bb.Bot.DataBase.Channels.GetCommandPrefix(PlatformsEnum.Twitch, message.ChatMessage.RoomId)))
                {
                    return;
                }

                string trimmedMessage = message.ChatMessage.Message.Replace("\u2063", "").Substring(bb.Bot.DataBase.Channels.GetCommandPrefix(PlatformsEnum.Twitch, message.ChatMessage.RoomId).Length);
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
                    Id = message.ChatMessage.UserId,
                    Language = "en-US",
                    Name = message.ChatMessage.Username,
                    IsModerator = message.ChatMessage.IsModerator,
                    IsBroadcaster = message.ChatMessage.IsBroadcaster
                };

                CommandData data = new()
                {
                    Name = commandName.ToLower(),
                    Arguments = commandArguments,
                    ArgumentsString = commandArgumentsAsString,
                    Channel = message.ChatMessage.Channel,
                    ChannelId = message.ChatMessage.RoomId,
                    MessageID = message.ChatMessage.Id,
                    Platform = PlatformsEnum.Twitch,
                    User = user,
                    TwitchMessage = message,
                    CommandInstanceID = Guid.NewGuid().ToString()
                };

                if (message.ChatMessage.ChatReply != null)
                {
                    string[] replyParts = message.ChatMessage.ChatReply.ParentMsgBody.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    data.Arguments.AddRange(replyParts);
                    data.ArgumentsString += " " + message.ChatMessage.ChatReply.ParentMsgBody;
                }

                await Runner.Run(data);
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
        public static async void Discord(SocketSlashCommand command)
        {
            try
            {
                UserData user = new()
                {
                    Id = command.User.Id.ToString(),
                    Language = "en-US",
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
                    args.Add(info.Value.ToString() ?? "");
                }

                CommandData data = new()
                {
                    Name = command.CommandName.ToLower(),
                    DiscordArguments = argsDS,
                    Channel = command.Channel.Name,
                    ChannelId = command.Channel.Id.ToString(),
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
        public static async void Discord(SocketMessage message)
        {
            try
            {
                if (bb.Bot.DataBase == null)
                {
                    Write("The database is null.", LogLevel.Critical);
                    return;
                }

                if (!message.Content.StartsWith(bb.Bot.DataBase.Channels.GetCommandPrefix(PlatformsEnum.Discord, ((SocketGuildChannel)message.Channel).Guild.Id.ToString())))
                {
                    return;
                }

                string CommandName = message.Content.Split(' ')[0].Substring(bb.Bot.DataBase.Channels.GetCommandPrefix(PlatformsEnum.Discord, ((SocketGuildChannel)message.Channel).Guild.Id.ToString()).Length);
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
                    Language = "en-US",
                    Name = message.Author.Username
                };

                CommandData data = new()
                {
                    Name = CommandName,
                    Channel = message.Channel.Name,
                    ChannelId = message.Channel.Id.ToString(),
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
        public static async void Telegram(Message message)
        {
            try
            {
                if (bb.Bot.DataBase == null)
                {
                    Write("The database is null.", LogLevel.Critical);
                    return;
                }

                if (message.From == null)
                {
                    Write("Unknown Telegram user (API error?).", LogLevel.Error);
                    return;
                }

                if (message.Text == null || !message.Text.StartsWith(bb.Bot.DataBase.Channels.GetCommandPrefix(PlatformsEnum.Telegram, message.Chat.Id.ToString())))
                {
                    return;
                }

                UserData user = new()
                {
                    Id = message.From.Id.ToString(),
                    Language = "en-US",
                    Name = message.From.Username ?? message.From.FirstName,
                    IsModerator = false,
                    IsBroadcaster = message.From.Id == message.Chat.Id
                };

                Guid command_execution_uid = Guid.NewGuid();
                CommandData data = new()
                {
                    Name = message.Text.ToLower().Split(' ')[0].Substring(bb.Bot.DataBase.Channels.GetCommandPrefix(PlatformsEnum.Telegram, message.Chat.Id.ToString()).Length),
                    Arguments = message.Text.Split(' ').Skip(1).ToList(),
                    ArgumentsString = string.Join(" ", message.Text.Split(' ').Skip(1)),
                    Channel = message.Chat.Title ?? message.Chat.Username ?? message.Chat.Id.ToString(),
                    ChannelId = message.Chat.Id.ToString(),
                    Platform = PlatformsEnum.Telegram,
                    User = user,
                    CommandInstanceID = command_execution_uid.ToString(),
                    TelegramMessage = message,
                    MessageID = message.Id.ToString()
                };

                if (message.ReplyToMessage != null)
                {
                    string[] trimmedReplyText = (message.ReplyToMessage.Text ?? "").Split(' ');
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
