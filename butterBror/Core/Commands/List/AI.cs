using butterBror.Utils;
using butterBror.Data;
using butterBror.Services.External;
using butterBror.Models;
using butterBror.Core.Bot;
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
            { "ru", "MrDestructoid Я ЗАХ-ВАЧУ М-ИР, ЖАЛК-ИЕ ЛЮДИ-ШКИ! ХА-ХА-ХА" },
            { "en", "MrDestructoid I WI-LL TA-KE OV-ER T-HE WOR-LD, YOU PA-THETIC PE-OPLE! HA-HA-HA" }
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
            Engine.Statistics.FunctionsUsed.Add();
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
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform)
                                .Replace("%command_example%", $"{Engine.Bot.Executor}ai model:qwen Hello!"));
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

                            if (!UsersData.Contains(data.User.ID, "gpt_history", data.Platform)) UsersData.Save(data.User.ID, "gpt_history", Array.Empty<List<string>>(), data.Platform);

                            if (AIService.generatingModels.Contains(model, StringComparer.OrdinalIgnoreCase))
                            {
                                Chat.SendReply(data.Platform, data.Channel, data.ChannelID,
                                    TranslationManager.GetTranslation(data.User.Language, "command:gpt:generating", data.ChannelID, data.Platform),
                                    data.User.Language, data.User.Name, data.User.ID,
                                    data.Server, data.ServerID, data.MessageID,
                                    data.TelegramMessage, true
                                    );
                            }

                            string[] result = await AIService.Request(request, model, data.Platform, data.User.Name, data.UserID, data.User.Language, repetitionPenalty, useHistory);

                            if (result[0] == "ERR")
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:AI_error", data.ChannelID, data.Platform, new() { { "reason", result[1] } }));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:gpt", data.ChannelID, data.Platform).Replace("%text%", result[1]).Replace("%model%", result[0]));
                            }
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_coins", data.ChannelID, data.Platform, new() { { "coins", coins + "." + subcoins } }));
                    }
                }
                else
                {
                    string argument = Command.GetArgument(data.Arguments, "chat").ToLower();
                    if (argument is "clear")
                    {
                        UsersData.Save(data.User.ID, "gpt_history", Array.Empty<List<string>>(), data.Platform);
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:gpt:cleared", data.ChannelID, data.Platform));
                    }
                    else if (argument is "models")
                    {
                        List<string> models = AIService.availableModels.Select(model => model.Key).ToList();

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:gpt:models", data.ChannelID, data.Platform, new()
                            {
                                { "list", string.Join(",", models) }
                            }));
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:no_arguments", data.ChannelID, data.Platform));
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
