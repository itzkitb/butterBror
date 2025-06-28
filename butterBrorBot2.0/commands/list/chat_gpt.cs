using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;

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
                    if (Command.GetArgument(data.arguments, "chat") is null)
                    {
                        float currency = Core.BankDollars / Core.Coins;
                        float cost = 0.5f / currency;

                        int coins = -(int)cost;
                        int subcoins = -(int)((cost - coins) * 100);

                        if (Utils.Tools.Balance.GetBalance(data.user_id, data.platform) + Utils.Tools.Balance.GetSubbalance(data.user_id, data.platform) / 100f >= coins + subcoins / 100f)
                        {
                            if (data.arguments.Count < 1)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", data.channel_id, data.platform)
                                    .Replace("%command_example%", $"{Core.Bot.Executor}ai model:qwen Hello!"));
                            else
                            {
                                Utils.Tools.Balance.Add(data.user_id, coins, subcoins, data.platform);

                                string request = data.arguments_string;
                                string model = "qwen";
                                double repetitionPenalty = 1;
                                bool useHistory = true;

                                if (Command.GetArgument(data.arguments, "model") is not null)
                                {
                                    model = Command.GetArgument(data.arguments, "model");
                                    request = request.Replace($"model:{model}", "");
                                }

                                if (Command.GetArgument(data.arguments, "repetition_penalty") is not null)
                                {
                                    try
                                    {
                                        repetitionPenalty = Utils.Tools.Format.ToDouble(Command.GetArgument(data.arguments, "repetition_penalty"));
                                        request = request.Replace($"repetition_penalty:{repetitionPenalty}", "");

                                        if (repetitionPenalty > 2) repetitionPenalty = 2;
                                    }
                                    catch { }
                                }

                                if (Command.GetArgument(data.arguments, "history") is "ignore")
                                {
                                    useHistory = false;
                                    request = request.Replace("history:ignore", "");
                                }

                                if (!UsersData.Contains(data.user.id, "gpt_history", data.platform)) UsersData.Save(data.user.id, "gpt_history", Array.Empty<List<string>>(), data.platform);

                                if (Utils.Tools.API.AI.generating_models.Contains(model, StringComparer.OrdinalIgnoreCase))
                                {
                                    Chat.SendReply(data.platform, data.channel, data.channel_id,
                                        TranslationManager.GetTranslation(data.user.language, "command:gpt:generating", data.channel_id, data.platform),
                                        data.user.language, data.user.username, data.user.id,
                                        data.server, data.server_id, data.message_id,
                                        data.telegram_message, true
                                        );
                                }

                                string[] result = await Utils.Tools.API.AI.Request(request, model, data.platform, data.user.username, data.user_id, data.user.language, repetitionPenalty, useHistory);
                                
                                if (result[0] == "ERR")
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:AI_error", data.channel_id, data.platform, new() { { "reason", result[1] } }));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:gpt", data.channel_id, data.platform).Replace("%text%", result[1]).Replace("%model%", result[0]));
                                }
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:not_enough_coins", data.channel_id, data.platform, new() { { "coins", coins + "." + subcoins } }));
                        }
                    }
                    else
                    {
                        string argument = Command.GetArgument(data.arguments, "chat").ToLower();
                        if (argument is "clear")
                        {
                            UsersData.Save(data.user.id, "gpt_history", Array.Empty<List<string>>(), data.platform);
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:gpt:cleared", data.channel_id, data.platform));
                        }
                        else if (argument is "models")
                        {
                            List<string> models = Utils.Tools.API.AI.available_models.Select(model => model.Key).ToList();

                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:gpt:models", data.channel_id, data.platform, new()
                            {
                                { "list", string.Join(",", models) }
                            }));
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:no_arguments", data.channel_id, data.platform));
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
