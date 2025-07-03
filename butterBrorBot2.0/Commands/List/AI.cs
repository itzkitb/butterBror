using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class AI_CHATBOT
        {
            public static CommandInfo Info = new()
            {
                Name = "AI",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "MrDestructoid Я ЗАХ-ВАЧУ М-ИР, ЖАЛК-ИЕ ЛЮДИ-ШКИ! ХА-ХА-ХА" },
                    { "en", "MrDestructoid I WI-LL TA-KE OV-ER T-HE WOR-LD, YOU PA-THETIC PE-OPLE! HA-HA-HA" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=gpt",
                CooldownPerUser = 30,
                CooldownPerChannel = 10,
                Aliases = ["gpt", "гпт", "chatgpt", "чатгпт", "джипити", "neuro", "нейро", "нейросеть", "neuralnetwork", "gwen", "ai", "ии"],
                Arguments = "[(model:gemma) (history:ignore) text/chat:clear/chat:models]",
                CooldownReset = false,
                CreationDate = DateTime.Parse("04/07/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public async Task<CommandReturn> Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (Command.GetArgument(data.Arguments, "chat") is null)
                    {
                        float currency = Core.BankDollars / Core.Coins;
                        float cost = 0.5f / currency;

                        int coins = -(int)cost;
                        int subcoins = -(int)((cost - coins) * 100);

                        if (Utils.Tools.Balance.GetBalance(data.UserID, data.Platform) + Utils.Tools.Balance.GetSubbalance(data.UserID, data.Platform) / 100f >= coins + subcoins / 100f)
                        {
                            if (data.Arguments.Count < 1)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform)
                                    .Replace("%command_example%", $"{Core.Bot.Executor}ai model:qwen Hello!"));
                            else
                            {
                                Utils.Tools.Balance.Add(data.UserID, coins, subcoins, data.Platform);

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
                                        repetitionPenalty = Utils.Tools.Format.ToDouble(Command.GetArgument(data.Arguments, "repetition_penalty"));
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

                                if (Utils.Tools.API.AI.generatingModels.Contains(model, StringComparer.OrdinalIgnoreCase))
                                {
                                    Chat.SendReply(data.Platform, data.Channel, data.ChannelID,
                                        TranslationManager.GetTranslation(data.User.Language, "command:gpt:generating", data.ChannelID, data.Platform),
                                        data.User.Language, data.User.Username, data.User.ID,
                                        data.Server, data.ServerID, data.MessageID,
                                        data.TelegramMessage, true
                                        );
                                }

                                string[] result = await Utils.Tools.API.AI.Request(request, model, data.Platform, data.User.Username, data.UserID, data.User.Language, repetitionPenalty, useHistory);
                                
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
                            List<string> models = Utils.Tools.API.AI.availableModels.Select(model => model.Key).ToList();

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
}
