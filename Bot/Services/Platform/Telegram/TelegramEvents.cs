using bb.Core.Commands;
using bb.Core.Configuration;
using bb.Models.Platform;
using bb.Utils;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using static bb.Core.Bot.Console;
using TelegramLib = Telegram;

namespace bb.Services.Platform.Telegram
{
    /// <summary>
    /// Contains event handlers for Telegram bot interactions and message processing.
    /// </summary>
    public partial class TelegramEvents
    {
        /// <summary>
        /// Handles incoming Telegram updates and processes user commands.
        /// </summary>
        /// <param name="client">The Telegram bot client instance.</param>
        /// <param name="update">The incoming update containing message data.</param>
        /// <param name="cancellation_token">Token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Processes commands like /start, /ping, /help, and routes them to appropriate handlers.
        /// Interacts with translation system and command processing components.
        /// </remarks>

        public static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken cancellation_token)
        {
            try
            {
                if (update.Type is not UpdateType.Message) return;

                Message message = update.Message;
                TelegramLib.Bot.Types.Chat chat = message.Chat;
                User user = message.From;
                User botData = bb.Program.BotInstance.Clients.Telegram.GetMe().Result;
                string text = message.Text;

                await bb.Program.BotInstance.MessageProcessor.ProcessMessageAsync(
                    user.Id.ToString(),
                    chat.Id.ToString(),
                    user.Username.ToLower(),
                    text == null ? "[ No text ]" : text,
                    null,
                    chat.Title == null ? botData.Username : chat.Title,
                    PlatformsEnum.Telegram,
                    message,
                    message.Id.ToString(),
                    null,
                    null);

                string lang = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(PlatformsEnum.Telegram, user.Id, Users.Language) ?? "en-US";

                if (MessageProcessor.IsEqualsSlashCommand("start", text, botData.Username))
                {
                    await client.SendMessage(chat.Id, LocalizationService.GetString(
                        lang,
                        "telegram:welcome",
                        chat.Id.ToString(),
                        PlatformsEnum.Telegram,
                        bb.Program.BotInstance.Version,
                        user.Id,
                        TextSanitizer.FormatTimeSpan(DateTime.UtcNow - bb.Program.BotInstance.StartTime, lang),
                        new Ping().Send(URLs.telegram, 1000).RoundtripTime), replyParameters: message.MessageId, cancellationToken: cancellation_token);
                }
                else if (MessageProcessor.IsEqualsSlashCommand("ping", text, botData.Username))
                {
                    var workTime = DateTime.UtcNow - bb.Program.BotInstance.StartTime;
                    PingReply reply = new Ping().Send(URLs.telegram, 1000);
                    string returnMessage = LocalizationService.GetString(
                        lang,
                        "command:ping",
                        chat.Id.ToString(),
                        PlatformsEnum.Telegram,
                        bb.Program.BotInstance.Version,
                        TextSanitizer.FormatTimeSpan(workTime, lang),
                        bb.Program.BotInstance.Clients.Twitch.JoinedChannels.Count + bb.Program.BotInstance.Clients.Discord.Guilds.Count + " (Twitch, Discord)",
                        Runner.commandInstances.Count.ToString(),
                        bb.Program.BotInstance.CompletedCommands.ToString(),
                        reply.RoundtripTime.ToString());

                    await client.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId,
                        cancellationToken: cancellation_token
                    );
                }
                else if (MessageProcessor.IsEqualsSlashCommand("help", text, botData.Username))
                {
                    string returnMessage = LocalizationService.GetString(lang, "text:bot_info", chat.Id.ToString(), PlatformsEnum.Telegram);
                    await client.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId,
                        cancellationToken: cancellation_token
                    );
                }
                else if (MessageProcessor.IsEqualsSlashCommand("commands", text, botData.Username))
                {
                    string returnMessage = LocalizationService.GetString(lang, "command:help", chat.Id.ToString(), PlatformsEnum.Telegram);
                    await client.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId,
                        cancellationToken: cancellation_token
                    );
                }

                bb.Program.BotInstance.CommandExecutor.Telegram(message);

                return;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Handles exceptions from the Telegram bot API client.
        /// </summary>
        /// <param name="botClient">The Telegram bot client instance.</param>
        /// <param name="error">The exception that occurred.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Formats and logs Telegram API errors, including HTTP status codes and API-specific exceptions.
        /// </remarks>

        public static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Write(error);
            return Task.CompletedTask;
        }
    }
}
