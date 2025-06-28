using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Things;
using butterBror.Utils.Tools;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TwitchLib.Client.Events;
using static butterBror.Utils.Things.Console;
using static butterBror.Utils.Tools.Text;

namespace butterBror.Utils
{
    public partial class TelegramEvents
    {
        [ConsoleSector("butterBror.Utils.TelegramEvents", "UpdateHandler")]
        public static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken cancellation_token)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (update.Type is not UpdateType.Message) return;

                Message message = update.Message;
                User user = message.From;
                User my_data = Core.Bot.Clients.Telegram.GetMe().Result;
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
                        { "WorkTime", FormatTimeSpan(DateTime.Now - Core.StartTime, lang) },
                        { "Version", Core.Version },
                        { "Ping", new Ping().Send(URLs.telegram, 1000).RoundtripTime.ToString() } }), replyParameters: message.MessageId
, cancellationToken: cancellation_token);
                }
                else if (text.StartsWith("/ping", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("/ping@" + my_data.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var workTime = DateTime.Now - Core.StartTime;
                    PingReply reply = new Ping().Send(URLs.telegram, 1000);
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:ping", chat.Id.ToString(), Platforms.Telegram, new(){
                        { "version", Core.Version },
                        { "workTime", FormatTimeSpan(workTime, lang) },
                        { "tabs", (Core.Bot.Clients.Twitch.JoinedChannels.Count + Core.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)" },
                        { "loadedCMDs", Commands.commands.Count().ToString() },
                        { "completedCMDs", Core.CompletedCommands.ToString() },
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
                else if (text.StartsWith(Core.Bot.Executor))
                {
                    text = text[1..];
                    Commands.Telegram(message);
                }

                return;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.TelegramEvents", "ErrorHandler")]
        public static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            Core.Statistics.FunctionsUsed.Add();
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
