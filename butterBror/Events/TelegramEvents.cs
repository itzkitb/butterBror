using butterBror.Core.Bot;
using butterBror.Core.Commands;
using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TwitchLib.Client.Events;
using static butterBror.Core.Bot.Console;

namespace butterBror.Events
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
        [ConsoleSector("butterBror.Utils.TelegramEvents", "UpdateHandler")]
        public static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken cancellation_token)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                if (update.Type is not UpdateType.Message) return;

                Message message = update.Message;
                User user = message.From;
                User my_data = Engine.Bot.Clients.Telegram.GetMe().Result;
                string text = message.Text;

                Telegram.Bot.Types.Chat chat = message.Chat;

                await Command.ProcessMessageAsync(user.Id.ToString(), chat.Id.ToString(), user.Username.ToLower(), text == null ? "[ No text ]" : text, new OnMessageReceivedArgs(), chat.Title == null ? my_data.Username : chat.Title, PlatformsEnum.Telegram, message);

                string lang = UsersData.Get<string>(user.Id.ToString(), "language", PlatformsEnum.Telegram);
                lang ??= "ru";

                if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("/start@" + my_data.Username, StringComparison.OrdinalIgnoreCase))
                {
                    await client.SendMessage(chat.Id, TranslationManager.GetTranslation(lang, "telegram:welcome", chat.Id.ToString(), PlatformsEnum.Telegram, new() {
                        { "ID", user.Id.ToString() },
                        { "WorkTime", Text.FormatTimeSpan(DateTime.Now - Engine.StartTime, lang) },
                        { "Version", Engine.Version },
                        { "Ping", new Ping().Send(URLs.telegram, 1000).RoundtripTime.ToString() } }), replyParameters: message.MessageId
, cancellationToken: cancellation_token);
                }
                else if (text.StartsWith("/ping", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("/ping@" + my_data.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var workTime = DateTime.Now - Engine.StartTime;
                    PingReply reply = new Ping().Send(URLs.telegram, 1000);
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:ping", chat.Id.ToString(), PlatformsEnum.Telegram, new(){
                        { "version", Engine.Version },
                        { "workTime", Text.FormatTimeSpan(workTime, lang) },
                        { "tabs", Engine.Bot.Clients.Twitch.JoinedChannels.Count + Engine.Bot.Clients.Discord.Guilds.Count + " (Twitch, Discord)" },
                        { "loadedCMDs", Runner.commandInstances.Count.ToString() },
                        { "completedCMDs", Engine.CompletedCommands.ToString() },
                        { "ping", reply.RoundtripTime.ToString() }
                    });
                    await client.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId,
                        cancellationToken: cancellation_token
                    );
                }
                else if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("/help@" + my_data.Username, StringComparison.OrdinalIgnoreCase))
                {
                    string returnMessage = TranslationManager.GetTranslation(lang, "text:bot_info", chat.Id.ToString(), PlatformsEnum.Telegram);
                    await client.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId,
                        cancellationToken: cancellation_token
                    );
                }
                else if (text.StartsWith("/commands", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("/commands@" + my_data.Username, StringComparison.OrdinalIgnoreCase))
                {
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:help", chat.Id.ToString(), PlatformsEnum.Telegram);
                    await client.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId,
                        cancellationToken: cancellation_token
                    );
                }
                else if (text.StartsWith(Engine.Bot.Executor))
                {
                    text = text[1..];
                    Executor.Telegram(message);
                }

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
        [ConsoleSector("butterBror.Utils.TelegramEvents", "ErrorHandler")]
        public static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            Engine.Statistics.FunctionsUsed.Add();
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
