using butterBror.Utils.DataManagers;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using TwitchLib.Client.Events;
using static butterBror.Utils.Things.Console;
using butterBror.Utils.Things;
using butterBror.Utils.Tools;

namespace butterBror.Utils.Workers
{
    public class Telegram
    {
        [ConsoleSector("butterBror.Utils.Workers.Telegram", "UpdateHandler")]
        public static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (update.Type != UpdateType.Message) return;

                Message message = update.Message;
                User user = message.From;
                global::Telegram.Bot.Types.Chat chat = message.Chat;
                User meData = Core.Bot.Clients.Telegram.GetMe().Result;

                await Command.ProcessMessageAsync(user.Id.ToString(), chat.Id.ToString(), user.Username.ToLower(), (message.Text == null ? "[ Not text ]" : message.Text), new OnMessageReceivedArgs(), (chat.Title == null ? meData.Username : chat.Title), Platforms.Telegram, message);

                string lang = UsersData.Get<string>(user.Id.ToString(), "language", Platforms.Telegram);
                if (lang == null) lang = "ru";

                if (message.Text == "/start" || message.Text == "/start@" + meData.Username)
                {
                    await botClient.SendMessage(
                        chat.Id,
                        TranslationManager.GetTranslation(lang, "telegram:welcome", chat.Id.ToString(), Platforms.Telegram, new() {
                        { "ID", user.Id.ToString() },
                        { "WorkTime", Text.FormatTimeSpan(DateTime.Now - Core.StartTime, lang) },
                        { "Version", Core.Version },
                        { "Ping", new System.Net.NetworkInformation.Ping().Send(URLs.telegram, 1000).RoundtripTime.ToString() } }),
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text == "/ping" || message.Text == "/ping@" + meData.Username)
                {
                    var workTime = DateTime.Now - Core.StartTime;
                    long reply = await Tools.API.Telegram.Ping();
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:ping", chat.Id.ToString(), Platforms.Telegram, new(){
                        { "version", Core.Version },
                        { "workTime", Text.FormatTimeSpan(workTime, lang) },
                        { "tabs", (Core.Bot.Clients.Twitch.JoinedChannels.Count() + Core.Bot.Clients.Discord.Guilds.Count()).ToString() + " (Twitch, Discord)" },
                        { "loadedCMDs", Commands.commands.Count().ToString() },
                        { "completedCMDs", Core.CompletedCommands.ToString() },
                        { "ping", reply.ToString() }
                    });
                    await botClient.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text == "/help" || message.Text == "/help@" + meData.Username)
                {
                    string returnMessage = TranslationManager.GetTranslation(lang, "text:bot_info", chat.Id.ToString(), Platforms.Telegram);
                    await botClient.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text == "/commands" || message.Text == "/commands@" + meData.Username)
                {
                    string returnMessage = TranslationManager.GetTranslation(lang, "command:help", chat.Id.ToString(), Platforms.Telegram);
                    await botClient.SendMessage(
                        chat.Id,
                        returnMessage,
                        replyParameters: message.MessageId
                    );
                }
                else if (message.Text.StartsWith(Core.Bot.Executor))
                {
                    message.Text = message.Text[1..];
                    Commands.Telegram(message);
                }

                return;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Workers.Telegram", "ErrorHandler")]
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
