using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

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
                Arguments = "(text)",
                CooldownReset = false,
                CreationDate = DateTime.Parse("04/07/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public async Task<CommandReturn> Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (new NoBanwords().Check(data.arguments_string, data.channel_id, data.platform))
                    {
                        float currency = Engine.BankDollars / Engine.Coins;
                        float cost = 0.5f / currency;

                        int coins = -(int)cost;
                        int subcoins = -(int)((cost - coins) * 100);
                        Utils.Console.WriteLine("cu:" + currency + ",ct:" + cost + ",cs:" + coins + ",s:" + subcoins, "info");

                        if (Utils.Balance.GetBalance(data.user_id, data.platform) + Utils.Balance.GetSubbalance(data.user_id, data.platform) / 100f >= coins + subcoins / 100f)
                        {
                            if (data.arguments.Count < 1)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", data.channel_id, data.platform)
                                    .Replace("%command_example%", $"{Maintenance.executor}ai model:qwen Hello!"));
                            else
                            {
                                Utils.Balance.Add(data.user_id, coins, subcoins, data.platform);

                                string request = data.arguments_string;
                                string model = null;

                                if (Command.GetArgument(data.arguments, "model") is not null)
                                {
                                    model = Command.GetArgument(data.arguments, "model");
                                    request = request.Replace($"model:{model}", "");
                                }

                                string[] result = await Utils.API.AI.Request(request, model, data.platform, data.user.username, data.user_id, data.user.language);
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
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:message_could_not_be_sent", data.channel_id, data.platform));
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
