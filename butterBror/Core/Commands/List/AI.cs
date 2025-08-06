using butterBror.Core.Bot;
using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Models;
using butterBror.Services.External;
using butterBror.Utils;
using Microsoft.CodeAnalysis;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class AI : CommandBase
    {
        public override string Name => "AI";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/AI.cs";
        public override Version Version => new Version("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "MrDestructoid Я ЗАХ-ВАЧУ М-ИР, ЖАЛК-ИЕ ЛЮДИ-ШКИ! ХА-ХА-ХА" },
            { "en-US", "MrDestructoid I WI-LL TA-KE OV-ER T-HE WOR-LD, YOU PA-THETIC PE-OPLE! HA-HA-HA" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=gpt";
        public override int CooldownPerUser => 30;
        public override int CooldownPerChannel => 10;
        public override string[] Aliases => ["gpt", "гпт", "chatgpt", "чатгпт", "джипити", "neuro", "нейро", "нейросеть", "neuralnetwork", "gwen", "ai", "ии"];
        public override string HelpArguments => "[(model:gemma) (history:ignore) text/chat:clear/chat:models]";
        public override DateTime CreationDate => DateTime.Parse("04/07/2024");
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyBotModerator => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (Command.GetArgument(data.Arguments, "chat") is null)
                {
                    float currency = Engine.BankDollars / Engine.Coins;
                    float cost = 0.5f / currency;

                    int coins = -(int)cost;
                    int subcoins = -(int)((cost - coins) * 100);

                    if (Utils.Balance.GetBalance(data.UserID, data.Platform) + Utils.Balance.GetSubbalance(data.UserID, data.Platform) / 100f >= coins + subcoins / 100f)
                    {
                        if (data.Arguments.Count < 1)
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", data.ChannelId, data.Platform, $"{Engine.Bot.Executor}ai model:qwen Hello!"));
                        else
                        {
                            Utils.Balance.Add(data.UserID, coins, subcoins, data.Platform);

                            string request = data.ArgumentsString;
                            string model = "qwen";
                            double repetitionPenalty = 1;
                            bool useHistory = true;

                            if (Command.GetArgument(data.Arguments, "model") is not null)
                            {
                                model = Command.GetArgument(data.Arguments, "model");
                                request = request.Replace($"model:{model}", "");
                            }

                            if (Command.GetArgument(data.Arguments, "repetition_penalty") is not null)
                            {
                                try
                                {
                                    repetitionPenalty = Utils.Format.ToDouble(Command.GetArgument(data.Arguments, "repetition_penalty"));
                                    request = request.Replace($"repetition_penalty:{repetitionPenalty}", "");

                                    if (repetitionPenalty > 2) repetitionPenalty = 2;
                                }
                                catch { }
                            }

                            if (Command.GetArgument(data.Arguments, "history") is "ignore")
                            {
                                useHistory = false;
                                request = request.Replace("history:ignore", "");
                            }

                            if (AIService.generatingModels.Contains(model, StringComparer.OrdinalIgnoreCase))
                            {
                                Utils.Chat.SendReply(data.Platform, data.Channel, data.ChannelId,
                                    LocalizationService.GetString(data.User.Language, "command:gpt:generating", data.ChannelId, data.Platform),
                                    data.User.Language, data.User.Name, data.User.ID,
                                    data.Server, data.ServerID, data.MessageID,
                                    data.TelegramMessage, true
                                    );
                            }

                            string[] result = await AIService.Request(request, model, data.Platform, data.User.Name, data.UserID, data.User.Language, repetitionPenalty, useHistory);

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
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_coins", data.ChannelId, data.Platform, coins + "." + subcoins));
                    }
                }
                else
                {
                    string argument = Command.GetArgument(data.Arguments, "chat").ToLower();
                    if (argument is "clear")
                    {
                        Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.User.ID), Users.GPTHistory, "[]");
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:gpt:cleared", data.ChannelId, data.Platform));
                    }
                    else if (argument is "models")
                    {
                        List<string> models = AIService.availableModels.Select(model => model.Key).ToList();

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
