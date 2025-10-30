using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Services.External;
using bb.Utils;
using Microsoft.CodeAnalysis;
using Telegram.Bot.Types;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class AI : CommandBase
    {
        public override string Name => "AI";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/AI.cs";
        public override Version Version => new Version("1.0.2");
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "MrDestructoid Я ЗАХ-ВАЧУ М-ИР, ЖАЛК-ИЕ ЛЮДИ-ШКИ! ХА-ХА-ХА" },
            { Language.EnUs, "MrDestructoid I WI-LL TA-KE OV-ER T-HE WOR-LD, YOU PA-THETIC PE-OPLE! HA-HA-HA" }
        };
        public override string WikiLink => $"{Program.WikiURL}gpt";
        public override int CooldownPerUser => 15;
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["gpt", "chatgpt", "neuro", "neuralnetwork", "ai", "ask", "question", "query", "llm", "think", "answer", "inquire"];
        public override string HelpArguments => "([chat:clear] [chat:models] / (model:<name>) (chat:ignore) <query>)";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyBotModerator => false;
        public override bool OnlyChannelModerator => false;
        public override Models.Platform.Platform[] Platforms => [Models.Platform.Platform.Twitch, Models.Platform.Platform.Telegram, Models.Platform.Platform.Discord];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.Arguments == null || bb.Program.BotInstance.UsersBuffer == null || data.Channel == null || data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                if (MessageProcessor.GetArgument(data.Arguments, "chat") is null)
                {
                    decimal currency = bb.Program.BotInstance.Coins == 0 ? 1 : bb.Program.BotInstance.InBankDollars / bb.Program.BotInstance.Coins;
                    decimal cost = currency == 0 ? 0.5m : 0.5m / currency;
                    Core.Bot.Logger.Write($"Currency: {currency}", Core.Bot.Logger.LogLevel.Debug);
                    Core.Bot.Logger.Write($"Cost: {cost}", Core.Bot.Logger.LogLevel.Debug);

                    if (bb.Program.BotInstance.Currency.GetBalance(data.User.Id, data.Platform) >= cost)
                    {
                        if (data.Arguments.Count < 1)
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", data.ChannelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}ai model:qwen Hello!"));
                        else
                        {
                            bb.Program.BotInstance.Currency.Add(data.User.Id, -cost, data.Platform);

                            string request = data.ArgumentsString;
                            string model = "qwen";
                            double repetitionPenalty = 1;
                            bool useHistory = true;

                            if (MessageProcessor.GetArgument(data.Arguments, "model") is not null)
                            {
                                model = MessageProcessor.GetArgument(data.Arguments, "model");
                                request = request.Replace($"model:{model}", "");
                            }

                            if (MessageProcessor.GetArgument(data.Arguments, "repetition_penalty") is not null)
                            {
                                try
                                {
                                    repetitionPenalty = Utils.DataConversion.ToDouble(MessageProcessor.GetArgument(data.Arguments, "repetition_penalty"));
                                    request = request.Replace($"repetition_penalty:{repetitionPenalty}", "");

                                    if (repetitionPenalty > 2) repetitionPenalty = 2;
                                }
                                catch { }
                            }

                            if (MessageProcessor.GetArgument(data.Arguments, "history") is "ignore")
                            {
                                useHistory = false;
                                request = request.Replace("history:ignore", "");
                            }

                            if (bb.Program.BotInstance.AiService.models.ContainsKey(model.ToLower()))
                            {
                                bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, LocalizationService.GetString(data.User.Language, "command:gpt:generating", data.ChannelId, data.Platform),
                                    data.Channel, data.ChannelId, data.User.Language, data.User.Name, data.User.Id, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, true, true, false);
                            }

                            string[] result = await bb.Program.BotInstance.AiService.Request(request, data.Platform, model, data.User.Name, data.User.Id, data.User.Language, repetitionPenalty, useHistory);

                            if (result[0] == "ERR")
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:AI_error", data.ChannelId, data.Platform, result[1]));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:gpt", data.ChannelId, data.Platform, result[0], result[1]));
                            }
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_coins", data.ChannelId, data.Platform, cost));
                    }
                }
                else
                {
                    string argument = MessageProcessor.GetArgument(data.Arguments, "chat").ToLower();
                    if (argument is "clear")
                    {
                        bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AiHistory, "[]");
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:gpt:cleared", data.ChannelId, data.Platform));
                    }
                    else if (argument is "models")
                    {
                        List<string> models = bb.Program.BotInstance.AiService.models.Select(model => model.Key).ToList();

                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:gpt:models", data.ChannelId, data.Platform, string.Join(",", models)));
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:no_arguments", data.ChannelId, data.Platform));
                    }
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
