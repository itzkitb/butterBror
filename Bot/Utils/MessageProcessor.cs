using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using TwitchLib.Client.Models;
using static bb.Core.Bot.Logger;

namespace bb.Utils
{
    /// <summary>
    /// Provides comprehensive command processing functionality for chatbot operations across multiple platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class handles the core message processing pipeline including:
    /// <list type="bullet">
    /// <item>Command argument parsing and validation</item>
    /// <item>User and global cooldown management</item>
    /// <item>Message rate limiting and concurrency control</item>
    /// <item>Currency reward distribution for user engagement</item>
    /// <item>Dynamic code execution for privileged users</item>
    /// <item>Cross-platform message normalization</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key design features:
    /// <list type="bullet">
    /// <item>Thread-safe operations through semaphore management</item>
    /// <item>Platform-agnostic implementation (Twitch, Discord, Telegram)</item>
    /// <item>Integrated cooldown system with VIP/moderator bypass</item>
    /// <item>Real-time user statistics tracking</item>
    /// <item>Secure code execution environment for debugging</item>
    /// </list>
    /// </para>
    /// All methods are designed for high-frequency execution in chatbot environments with minimal performance overhead.
    /// </remarks>
    public class MessageProcessor
    {
        public ulong Proccessed = 0;
        public readonly ConcurrentDictionary<string, (SemaphoreSlim Semaphore, DateTime LastUsed)> messagesSemaphores = new(StringComparer.Ordinal);
        private readonly Regex MentionRegex = new(@"@(\w+)", RegexOptions.Compiled);

        /// <summary>
        /// Retrieves a positional command argument from the provided list.
        /// </summary>
        /// <param name="args">List of command arguments to search through</param>
        /// <param name="index">Zero-based index of the desired argument</param>
        /// <returns>
        /// The argument at the specified index if available; otherwise <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Usage example:
        /// <code>
        /// // For command "!command arg1 arg2"
        /// var args = new List<string> { "arg1", "arg2" };
        /// string firstArg = Command.GetArgument(args, 0); // Returns "arg1"
        /// string thirdArg = Command.GetArgument(args, 2); // Returns null
        /// </code>
        /// </para>
        /// <para>
        /// Behavior:
        /// <list type="bullet">
        /// <item>Performs bounds checking to prevent <see cref="ArgumentOutOfRangeException"/></item>
        /// <item>Returns <see langword="null"/> for out-of-range indices (safer than exceptions)</item>
        /// <item>Maintains original argument casing and formatting</item>
        /// </list>
        /// </para>
        /// Preferred over direct list indexing for safer command argument handling.
        /// </remarks>
        public static string GetArgument(List<string> args, int index)
        {
            if (args.Count > index)
                return args[index];
            return null;
        }

        /// <summary>
        /// Retrieves a named command argument value from the provided list.
        /// </summary>
        /// <param name="args">List of command arguments to search through</param>
        /// <param name="name">Name prefix to identify the argument (e.g., "user")</param>
        /// <returns>
        /// The value portion after the colon for matching argument, or <see langword="null"/> if not found.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Pattern recognition:
        /// <list type="bullet">
        /// <item>Matches arguments in format "{arg_name}:value"</item>
        /// <item>Case-sensitive prefix matching (e.g., "user" matches "user:foo" but not "User:foo")</item>
        /// <item>Value portion includes everything after the colon</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage example:
        /// <code>
        /// // For command "!command user:John delay:5"
        /// var args = new List<string> { "user:John", "delay:5" };
        /// string user = Command.GetArgument(args, "user"); // Returns "John"
        /// string role = Command.GetArgument(args, "role"); // Returns null
        /// </code>
        /// </para>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Performs linear search through arguments</item>
        /// <item>First matching argument wins (no support for multiple values)</item>
        /// <item>Does not trim whitespace from values</item>
        /// </list>
        /// </para>
        /// Ideal for commands with named parameters where order is not significant.
        /// </remarks>
        public static string GetArgument(List<string> args, string name)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith(name + ":")) return arg.Replace(name + ":", "");
            }
            return null;
        }

        /// <summary>
        /// Determines if a message matches the format of a slash command for the specified bot.
        /// </summary>
        /// <param name="commandName">The command name to check for (without slash)</param>
        /// <param name="message">The full message text to analyze</param>
        /// <param name="botName">The bot's username for platform-specific addressing</param>
        /// <returns>
        /// <see langword="true"/> if the message starts with the command in either basic or bot-tagged format;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Recognizes two valid formats:
        /// <list type="bullet">
        /// <item>Basic: <c>/{commandName}</c> (e.g., "/help")</item>
        /// <item>Bot-tagged: <c>/{commandName}@{botName}</c> (e.g., "/help@mybot")</item>
        /// </list>
        /// </para>
        /// <para>
        /// Matching behavior:
        /// <list type="bullet">
        /// <item>Case-insensitive command name comparison</item>
        /// <item>Requires exact command name match at message start</item>
        /// <item>Supports platform-specific command addressing</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage context:
        /// <list type="bullet">
        /// <item>Primarily used in Telegram command processing</item>
        /// <item>Helps distinguish commands from regular messages</item>
        /// <item>Supports multiple bots in same chat channel</item>
        /// </list>
        /// </para>
        /// Does not validate command parameters - only checks command invocation format.
        /// </remarks>
        public static bool IsEqualsSlashCommand(string commandName, string message, string botName)
        {
            return message.StartsWith($"/{commandName}", StringComparison.OrdinalIgnoreCase)
                   || message.StartsWith($"/{commandName}@{botName}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Processes incoming chat messages with comprehensive user engagement tracking and reward systems.
        /// </summary>
        /// <param name="userId">Unique user identifier for the message sender</param>
        /// <param name="channelId">Channel/room identifier where message was sent</param>
        /// <param name="username">Display name of the message sender</param>
        /// <param name="message">Full message content text</param>
        /// <param name="twitchMessageContext">Twitch-specific message metadata (null for other platforms)</param>
        /// <param name="channel">Channel name for display purposes</param>
        /// <param name="platform">Target platform identifier</param>
        /// <param name="telegramMessageContext">Telegram-specific message metadata (null for other platforms)</param>
        /// <param name="messageId">Unique message identifier for the platform</param>
        /// <param name="server">Discord server name (optional)</param>
        /// <param name="serverId">Discord server identifier (optional)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>
        /// <para>
        /// Processing pipeline:
        /// <list type="number">
        /// <item>Acquires user-specific semaphore for thread safety</item>
        /// <item>Validates user against ban/ignore lists</item>
        /// <item>Registers new users or updates existing profiles</item>
        /// <item>Processes AFK return detection</item>
        /// <item>Distributes currency rewards for message length</item>
        /// <item>Handles mention-based rewards</item>
        /// <item>Updates user statistics and last seen time</item>
        /// <item>Persists message history for analytics</item>
        /// </list>
        /// </para>
        /// <para>
        /// Currency system:
        /// <list type="bullet">
        /// <item>Base reward: 1 coin per 6 characters + 1</item>
        /// <item>Mention reward: <see cref="Bot.CurrencyMentioned"/> per mentioned user</item>
        /// <item>Mentioner bonus: <see cref="Bot.CurrencyMentioner"/> per successful mention</item>
        /// </list>
        /// </para>
        /// <para>
        /// Data persistence:
        /// <list type="bullet">
        /// <item>First messages stored in <see cref="Bot.allFirstMessages"/></item>
        /// <item>Regular messages buffered in <see cref="Bot.MessagesBuffer"/></item>
        /// <item>Batch persistence at minute boundaries for performance</item>
        /// </list>
        /// </para>
        /// This method is the central processing point for all chat messages across all platforms.
        /// All exceptions are caught and logged without disrupting message flow.
        /// </remarks>
        public async Task ProcessMessageAsync(
            string userId,
            string channelId,
            string username,
            string message,
            ChatMessage twitchMessageContext,
            string channel,
            Platform platform,
            Message telegramMessageContext,
            string messageId,
            string server = "",
            string serverId = ""
        )
        {
            if (!bb.Program.BotInstance.Initialized) return;

            var now = DateTime.UtcNow;
            var semaphore = messagesSemaphores.GetOrAdd(userId, id => (new SemaphoreSlim(1, 1), now));
            try
            {
                await semaphore.Semaphore.WaitAsync().ConfigureAwait(false);
                messagesSemaphores.TryUpdate(userId, (semaphore.Semaphore, now), semaphore);

                if (!bb.Program.BotInstance.DataBase.Users.CheckUserExists(platform, DataConversion.ToLong(userId)))
                {
                    bb.Program.BotInstance.DataBase.Users.RegisterNewUser(platform, DataConversion.ToLong(userId), LanguageDetector.DetectLanguage(message), message, channel);
                    bb.Program.BotInstance.DataBase.Users.AddUsernameMapping(platform, DataConversion.ToLong(userId), username.ToLower());
                    bb.Program.BotInstance.Users++;
                }

                // Skip banned or ignored users
                if ((Roles)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.Role)) <= Roles.Bot)
                    return;

                DateTime now_utc = DateTime.UtcNow;
                Proccessed++;

                // Handle AFK return
                if (DataConversion.ToLong(bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.IsAfk).ToString()) == 1)
                {
                    ReturnFromAFK(userId, channelId, channel, username, messageId, telegramMessageContext, platform, message, server, serverId);
                }

                // Award coins
                int addCoins = message.Length / 6 + 1;
                bb.Program.BotInstance.Currency.Add(userId, addCoins / 100M, platform);

                bb.Program.BotInstance.UsersBuffer.IncrementGlobalMessageCountAndLenght(platform, DataConversion.ToLong(userId), message.Length);
                bb.Program.BotInstance.UsersBuffer.IncrementMessageCountInChannel(platform, DataConversion.ToLong(userId), platform == Platform.Discord ? serverId : channelId);

                // Mentions handling
                List<string> mentionedList = new List<string>();
                int addToUser = 0;

                foreach (Match m in MentionRegex.Matches(message))
                {
                    string mentioned = m.Groups[1].Value.TrimEnd(',');
                    string mentionedId = UsernameResolver.GetUserID(mentioned, platform);

                    if (!string.Equals(mentioned, username, StringComparison.OrdinalIgnoreCase)
                        && mentionedId != null && !mentionedList.Contains(mentionedId))
                    {
                        mentionedList.Add(mentionedId);
                        bb.Program.BotInstance.Currency.Add(mentionedId, bb.Program.BotInstance.CurrencyMentioned / 100M, platform);
                        addToUser += bb.Program.BotInstance.CurrencyMentioner;
                    }
                }

                if (addToUser > 0)
                {
                    bb.Program.BotInstance.Currency.Add(userId, addToUser / 100M, platform);
                }

                // Save user state
                bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userId), Users.LastMessage, message);
                bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userId), Users.LastChannel, channel);
                bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userId), Users.LastSeen, now_utc.ToString("o"));

                // Persist message history
                var msg = new Data.Entities.Message
                {
                    messageDate = now_utc,
                    messageText = message,
                    isMe = platform == Platform.Twitch && twitchMessageContext.IsMe,
                    isVip = platform == Platform.Twitch && twitchMessageContext.IsVip,
                    isTurbo = platform == Platform.Twitch && twitchMessageContext.IsTurbo,
                    isStaff = platform == Platform.Twitch && twitchMessageContext.IsStaff,
                    isPartner = platform == Platform.Twitch && twitchMessageContext.IsPartner,
                    isModerator = platform == Platform.Twitch && twitchMessageContext.IsModerator,
                    isSubscriber = platform == Platform.Twitch && twitchMessageContext.IsSubscriber,
                };

                if (bb.Program.BotInstance.DataBase.Channels.GetFirstMessage(platform, channelId, DataConversion.ToLong(userId)) is null && !bb.Program.BotInstance.allFirstMessages.Contains((platform, channelId, DataConversion.ToLong(userId), msg)))
                {
                    bb.Program.BotInstance.allFirstMessages.Add((platform, channelId, DataConversion.ToLong(userId), msg));
                }
                bb.Program.BotInstance.MessagesBuffer.Add(platform, channelId, DataConversion.ToLong(userId), msg);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
            finally
            {
                semaphore.Semaphore.Release();
            }
        }

        /// <summary>
        /// Processes a user's return from AFK (Away From Keyboard) status with context-aware messaging.
        /// </summary>
        /// <param name="userId">Unique user identifier across platforms</param>
        /// <param name="channelId">Channel identifier where user is returning</param>
        /// <param name="channel">Display name of the target channel</param>
        /// <param name="username">Username to mention in the response</param>
        /// <param name="messageId">Original message ID for reply context (Twitch)</param>
        /// <param name="messageReply">Telegram message object for reply context</param>
        /// <param name="platform">Target platform (Twitch, Discord, or Telegram)</param>
        /// <param name="messageContent">Content of the triggering message for language detection</param>
        /// <param name="server">Server/guild name (Discord-specific)</param>
        /// <param name="serverId">Server/guild identifier (Discord-specific)</param>
        /// <remarks>
        /// <para>
        /// The method performs the following sequence:
        /// <list type="number">
        /// <item>Determines user's preferred language or detects from message content</item>
        /// <item>Retrieves AFK type and duration from user data storage</item>
        /// <item>Selects appropriate localized message based on AFK duration and type</item>
        /// <item>Formats time elapsed with platform-appropriate time formatting</item>
        /// <item>Sends context-aware reply mentioning the user</item>
        /// <item>Clears AFK status in user data storage</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Applies banned word filtering to AFK messages before sending</item>
        /// <item>Formats time spans using user's language preferences</item>
        /// <item>Clears AFK status immediately after generating response</item>
        /// <item>Maintains original message casing while cleaning ASCII artifacts</item>
        /// </list>
        /// </para>
        /// This method is automatically triggered when a user sends their first message after setting AFK status.
        /// </remarks>
        public void ReturnFromAFK(string userId, string channelId, string channel, string username, string messageId, Telegram.Bot.Types.Message messageReply, Platform platform, string messageContent, string server, string serverId)
        {
            Language language = Language.EnUs;
            object dbLanguage = bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.Language);

            if (dbLanguage == null)
            {
                bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userId), Users.Language, LanguageDetector.DetectLanguage(messageContent));
            }
            else
            {
                language = (Language)DataConversion.ToInt(dbLanguage);
            }

            string? message = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.AfkMessage);
            if (!bb.Program.BotInstance.MessageFilter.Check(message, platform == Platform.Discord ? serverId : channelId, platform).Item1)
                return;

            string send = (TextSanitizer.CleanAsciiWithoutSpaces(message) == "" ? "" : ": \"" + message + "\"");

            TimeSpan timeElapsed = DateTime.UtcNow - DateTime.Parse((string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.AfkStartTime), null, DateTimeStyles.AdjustToUniversal);
            var afkType = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.AfkType);
            string translateKey = "";

            if (afkType == "draw")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 8) translateKey = "draw:2h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "draw:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "draw:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "draw:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "draw:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "draw:1y";
                else translateKey = "draw:default";
            }
            else if (afkType == "afk")
            {
                if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 14) translateKey = "default:8h";
                else if (timeElapsed.TotalHours >= 14 && timeElapsed.TotalDays < 1) translateKey = "default:14h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "default:1d";
                else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "default:3d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 9) translateKey = "default:7d";
                else if (timeElapsed.TotalDays >= 9 && timeElapsed.TotalDays < 31) translateKey = "default:9d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "default:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "default:1y";
                else translateKey = "default";
            }
            else if (afkType == "sleep")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "sleep:2h";
                else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "sleep:5h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 12) translateKey = "sleep:8h";
                else if (timeElapsed.TotalHours >= 12 && timeElapsed.TotalDays < 1) translateKey = "sleep:12h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "sleep:1d";
                else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "sleep:3d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "sleep:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "sleep:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "sleep:1y";
                else translateKey = "sleep:default";
            }
            else if (afkType == "rest")
            {
                if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "rest:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "rest:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "rest:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "rest:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "rest:1y";
                else translateKey = "rest:default";
            }
            else if (afkType == "lurk") translateKey = "lurk:default";
            else if (afkType == "study")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "study:2h";
                else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "study:5h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "study:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "study:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "study:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "study:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "study:1y";
                else translateKey = "study:default";
            }
            else if (afkType == "poop")
            {
                if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalHours < 1) translateKey = "poop:1m";
                else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "poop:1h";
                else if (timeElapsed.TotalHours >= 8) translateKey = "poop:8h";
                else translateKey = "poop:default";
            }
            else if (afkType == "shower")
            {
                if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalMinutes < 10) translateKey = "shower:1m";
                else if (timeElapsed.TotalMinutes >= 10 && timeElapsed.TotalHours < 1) translateKey = "shower:10m";
                else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "shower:1h";
                else if (timeElapsed.TotalHours >= 8) translateKey = "shower:8h";
                else translateKey = "shower:default";
            }
            string text = LocalizationService.GetString(language, "afk:" + translateKey, platform == Platform.Discord ? serverId : channelId, platform, username); // FIX AA0
            bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userId), Users.AfkResume, DateTime.UtcNow.ToString("o"));
            bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userId), Users.IsAfk, 0);

            string reply = text + send + " (" + TextSanitizer.FormatTimeSpan(timeElapsed, language) + ")";
            bb.Program.BotInstance.MessageSender.Send(platform, reply, channel, channelId, language, username, userId, server, serverId, messageId, messageReply, false, false, false);
        }
    }
}
