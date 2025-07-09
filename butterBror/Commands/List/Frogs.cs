using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;
using DankDB;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class FrogGame
        {
            public static CommandInfo Info = new()
            {
                Name = "Frogs",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() {
                    { "ru", "Л Я Г У Ш К А" },
                    { "en", "F R O G" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=frogs",
                CooldownPerUser = 10,
                CooldownPerChannel = 1,
                Aliases = ["frog", "frg", "лягушка", "лягушки", "лягуш"],
                Arguments = "[(info)/(statistic)/(caught)/(gift [user] [frogs])]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("13/10/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string root_path = Engine.Bot.Pathes.Main + $"GAMES_DATA/{Platform.strings[(int)data.Platform]}/FROGS/";
                    string user_path = $"{root_path}{data.UserID}.json";

                    FileUtil.CreateDirectory(root_path);
                    
                    if (!File.Exists(user_path))
                    {
                        Manager.CreateDatabase(user_path);
                        SafeManager.Save(user_path, "frogs", 0, false);
                        SafeManager.Save(user_path, "gifted", 0, false);
                        SafeManager.Save(user_path, "received", 0);
                    }

                    long balance = Manager.Get<long>(user_path, "frogs");
                    long gifted = Manager.Get<long>(user_path, "gifted");
                    long received = Manager.Get<long>(user_path, "received");
                    string[] info_aliases = ["info", "i"];
                    string[] statistic_aliases = ["statistic", "stat", "s"];
                    string[] caught_aliases = ["caught", "c"];
                    string[] gift_aliases = ["gift", "g"];
                    string[] top_aliases = ["top", "t"];

                    if (data.Arguments != null && data.Arguments.Count > 0)
                    {
                        if (info_aliases.Contains(data.Arguments[0].ToLower()))
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:info", data.ChannelID, data.Platform));
                        }
                        else if (statistic_aliases.Contains(data.Arguments[0].ToLower()))
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:statistic", data.ChannelID, data.Platform)
                                .Replace("%frogs%", balance.ToString())
                                .Replace("%frogs_gifted%", gifted.ToString())
                                .Replace("%frogs_received%", received.ToString()));
                        }
                        else if (caught_aliases.Contains(data.Arguments[0].ToLower()))
                        {
                            if (Command.CheckCooldown(3600, 0, "frogs_reseter", data.UserID, data.ChannelID, data.Platform, false, true, true))
                            {
                                Random rand = new Random();
                                long frog_caught_type = rand.Next(0, 4);
                                long frogs_caughted = rand.Next(1, 11);

                                if (frog_caught_type == 0)
                                    frogs_caughted = rand.Next(1, 11);
                                else if (frog_caught_type <= 2)
                                    frogs_caughted = rand.Next(11, 101);
                                else if (frog_caught_type == 3)
                                    frogs_caughted = rand.Next(101, 1001);

                                string text = GetFrogRange(frogs_caughted);

                                if (text == "error:range")
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:error:range", data.ChannelID, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:caught", data.ChannelID, data.Platform)
                                        .Replace("%action%", TranslationManager.GetTranslation(data.User.Language, $"command:frog:won:{text}", data.ChannelID, data.Platform))
                                        .Replace("%old_frogs_count%", balance.ToString())
                                        .Replace("%new_frogs_count%", (balance + frogs_caughted).ToString())
                                        .Replace("%added_count%", frogs_caughted.ToString()));
                                    SafeManager.Save(user_path, "frogs", balance + frogs_caughted);
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:error:caught", data.ChannelID, data.Platform)
                                    .Replace("%time%", Text.FormatTimeSpan(Command.GetCooldownTime(data.UserID, "frogs_reseter", 3600, data.Platform), data.User.Language)));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else if (gift_aliases.Contains(data.Arguments[0].ToLower()))
                        {
                            if (data.Arguments.Count > 2)
                            {
                                string username = data.Arguments[1].ToLower();
                                long frogs = Utils.Tools.Format.ToLong(data.Arguments[2]);
                                string user_id = Names.GetUserID(username, data.Platform);

                                if (user_id == null)
                                {
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform),
                                        "%user%", Names.DontPing(username)));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                                else if (frogs == 0)
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:gift:error:zero", data.ChannelID, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                                else if (frogs < 0)
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:gift:error:lowerThanZero", data.ChannelID, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                                else if (balance >= frogs)
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:frog:gift", data.ChannelID, data.Platform)
                                        .Replace("%frogs%", frogs.ToString())
                                        .Replace("%gift_user%", Names.DontPing(username)));
                                    string gift_user_path = $"{root_path}{user_id}.json";

                                    if (!File.Exists(gift_user_path))
                                    {
                                        Manager.CreateDatabase(gift_user_path);
                                        SafeManager.Save(gift_user_path, "frogs", 0, false);
                                        SafeManager.Save(gift_user_path, "gifted", 0, false);
                                        SafeManager.Save(gift_user_path, "received", 0);
                                    }

                                    long gift_user_frogs_balance = Manager.Get<long>(gift_user_path, "frogs");
                                    long gift_user_received = Manager.Get<long>(gift_user_path, "received");

                                    SafeManager.Save(gift_user_path, "frogs", gift_user_frogs_balance + frogs, false);
                                    SafeManager.Save(gift_user_path, "received", gift_user_received + frogs, false);
                                    SafeManager.Save(user_path, "frogs", balance - frogs, false);
                                    SafeManager.Save(user_path, "gifted", gifted + frogs);
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "frog:gift:error:noFrogs", data.ChannelID, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform),
                                    "command_example", $"{Engine.Bot.Executor}frog gift [user] [frogs]"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:incorrect_parameters", data.ChannelID, data.Platform)); // Fix AB7
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform),
                                    "command_example", $"#frog {Info.Arguments}"));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }

            string GetFrogRange(long number)
            {
                var ranges = new Dictionary<string, Tuple<long, long>>
                    {
                        {"1", Tuple.Create(1L, 1L)},
                        {"2", Tuple.Create(2L, 2L)},
                        {"3", Tuple.Create(3L, 3L)},
                        {"4-6", Tuple.Create(4L, 6L)},
                        {"7-10", Tuple.Create(7L, 10L)},
                        {"11", Tuple.Create(11L, 11L)},
                        {"12-25", Tuple.Create(12L, 25L)},
                        {"26-50", Tuple.Create(26L, 50L)},
                        {"51-75", Tuple.Create(51L, 75L)},
                        {"76-100", Tuple.Create(76L, 100L)},
                        {"101-200", Tuple.Create(101L, 200L)},
                        {"201-300", Tuple.Create(201L, 300L)},
                        {"301-400", Tuple.Create(301L, 400L)},
                        {"401-500", Tuple.Create(401L, 500L)},
                        {"501-600", Tuple.Create(501L, 600L)},
                        {"601-700", Tuple.Create(601L, 700L)},
                        {"701-800", Tuple.Create(701L, 800L)},
                        {"801-900", Tuple.Create(801L, 900L)},
                        {"901-1000", Tuple.Create(901L, 1000L)},
                    };

                foreach (var range in ranges)
                {
                    if (number >= range.Value.Item1 && number <= range.Value.Item2)
                    {
                        return range.Key;
                    }
                }

                return "error:range";
            }
        }
    }
}
