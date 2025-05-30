using butterBror.Utils;
using butterBror.Utils.DataManagers;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TwitchLib.Client.Events;

namespace butterBror.BotUtils
{
    public partial class telegram_events
    {
        public static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken cancellation_token)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                if (update.Type is not UpdateType.Message) return;

                Message message = update.Message;
                User user = message.From;
                User my_data = Maintenance.telegram_client.GetMe().Result;
                string text = message.Text;

                Telegram.Bot.Types.Chat chat = message.Chat;

                await Command.ProcessMessageAsync(user.Id.ToString(), chat.Id.ToString(), user.Username.ToLower(), (text == null ? "[ No text ]" : text), new OnMessageReceivedArgs(), (chat.Title == null ? my_data.Username : chat.Title), Platforms.Telegram, message);

                string lang = UsersData.Get<string>(user.Id.ToString(), "language", Platforms.Telegram);
                lang ??= "ru";

                if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("/start@" + my_data.Username, StringComparison.OrdinalIgnoreCase))
                {
                    await client.SendMessage(chat.Id, TranslationManager.GetTranslation(lang, "telegram:welcome", chat.Id.ToString(), Platforms.Telegram, new() {
                        { "ID", user.Id.ToString() },
                        { "WorkTime", TextUtil.FormatTimeSpan(DateTime.Now - Engine.start_time, lang) },
                        { "Version", Engine.version },
                        { "Ping", new Ping().Send(Maintenance.telegram_url, 1000).RoundtripTime.ToString() } }), replyParameters: message.MessageId
, cancellationToken: cancellation_token);
                }
                else if (text.StartsWith("/ping", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("/ping@" + my_data.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var workTime = DateTime.Now - Engine.start_time;
                    PingReply reply = new Ping().Send(Maintenance.telegram_url, 1000);
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:ping", chat.Id.ToString(), Platforms.Telegram, new(){
                        { "version", Engine.version },
                        { "workTime", TextUtil.FormatTimeSpan(workTime, lang) },
                        { "tabs", Maintenance.channels_list.Length.ToString() },
                        { "loadedCMDs", Commands.commands.Count().ToString() },
                        { "completedCMDs", Engine.completed_commands.ToString() },
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
                    string returnMessage = TranslationManager.GetTranslation(lang, "text:bot_info", chat.Id.ToString(), Platforms.Telegram);
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
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:help", chat.Id.ToString(), Platforms.Telegram);
                    await client.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId,
                        cancellationToken: cancellation_token
                    );
                }
                else if (text.StartsWith(Maintenance.executor))
                {
                    text = text[1..];
                    Commands.Telegram(message);
                }

                return;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, "TelegramWorker\\UpdateHandler");
            }
        }
        public static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            Engine.Statistics.functions_used.Add();
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Utils.Console.WriteError(error, "TelegramWorker\\ErrorHandler");
            return Task.CompletedTask;
        }
    }
}
