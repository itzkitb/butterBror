using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class CustomTranslation
        {
            public static CommandInfo Info = new()
            {
                Name = "CustomTranslation",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Кастомизируй свой канал смешными сообщениями, не будь занудой!" },
                    { "en", "Customize your channel with funny messages, don't be a bore!" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=customtranslation",
                CooldownPerUser = 10,
                CooldownPerChannel = 5,
                Aliases = ["customtranslation", "ct", "кастомныйперевод", "кп"],
                Arguments = "(set [paramName] [en/ru] [text])/(get [paramName] [en/ru])/(original [paramName] [en/ru])/(reset [paramName] [en/ru])",
                CooldownReset = true,
                CreationDate = DateTime.Parse("08/05/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord],
                is_on_development = true
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string[] setAliases = ["set", "s", "установить", "сет", "с", "у"];
                    string[] getAliases = ["get", "g", "гет", "получить", "п", "г"];
                    string[] originalAliases = ["original", "оригинал", "о", "o"];
                    string[] deleteAliases = ["delete", "del", "d", "remove", "reset", "сбросить", "удалить", "с"];

                    string[] langs = ["ru", "en"];

                    string[] uneditableitems = ["text:bot_info", "cantSend", "lowArgs", "lang", "wrongArgs", "changedLang", "commandDoesntWork", "noneExistUser", "noAccess", "userBanned",
                    "userPardon", "rejoinedChannel", "joinedChannel", "leavedChannel", "modAdded", "modDel", "addedChannel", "delChannel", "welcomeChannel", "error", "botVerified",
                    "unhandledError", "Err"];

                    if (data.Arguments.Count >= 3)
                    {
                        try
                        {
                            string arg1 = data.Arguments[0];
                            string paramName = data.Arguments[1];
                            string lang = data.Arguments[2];
                            if (langs.Contains(lang))
                            {
                                if (setAliases.Contains(arg1) && ((bool)data.User.IsModerator || (bool)data.User.IsBroadcaster || (bool)data.User.IsBotModerator))
                                {
                                    if (data.Arguments.Count > 3)
                                    {
                                        List<string> textArgs = data.Arguments;
                                        textArgs = textArgs.Skip(3).ToList();
                                        string text = string.Join(' ', textArgs);
                                        if (uneditableitems.Contains(paramName))
                                        {
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:translation_secured", "", data.Platform));
                                            commandReturn.SetColor(ChatColorPresets.Red);
                                        }
                                        else
                                        {
                                            if (TranslationManager.TranslateContains(paramName))
                                            {
                                                if (TranslationManager.SetCustomTranslation(paramName, text, data.ChannelID, lang, data.Platform))
                                                {
                                                    TranslationManager.UpdateTranslation(lang, data.ChannelID, data.Platform);
                                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "command:custom_translation:set", "", data.Platform),
                                                        "key", paramName));
                                                }
                                                else
                                                {
                                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:translation_set", "", data.Platform));
                                                    commandReturn.SetColor(ChatColorPresets.Red);
                                                }
                                            }
                                            else
                                            {
                                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:translation_key_is_not_exist", "", data.Platform));
                                                commandReturn.SetColor(ChatColorPresets.GoldenRod);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", "", data.Platform),
                                            "command_example", "#ct set [paramName] [en/ru] [text]"));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                                else if (getAliases.Contains(arg1) && ((bool)data.User.IsModerator || (bool)data.User.IsBroadcaster || (bool)data.User.IsBotModerator))
                                {
                                    if (TranslationManager.TranslateContains(paramName))
                                    {
                                        commandReturn.SetMessage(Text.ArgumentsReplacement(TranslationManager.GetTranslation(data.User.Language, "command:custom_translation:get", "", data.Platform),
                                            new(){
                                                { "key", paramName },
                                                { "translation", TranslationManager.GetTranslation(lang, paramName, data.ChannelID, data.Platform) }
                                            }));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:translation_key_is_not_exist", "", data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.GoldenRod);
                                    }
                                }
                                else if (originalAliases.Contains(arg1))
                                {
                                    commandReturn.SetMessage(Text.ArgumentsReplacement(TranslationManager.GetTranslation(data.User.Language, "command:custom_translation:original", "", data.Platform),
                                        new(){
                                            { "key", paramName },
                                            { "translation", TranslationManager.GetTranslation(lang, paramName, string.Empty, data.Platform) }
                                       }));
                                }
                                else if (deleteAliases.Contains(arg1) && ((bool)data.User.IsModerator || (bool)data.User.IsBroadcaster || (bool)data.User.IsBotModerator))
                                {
                                    if (TranslationManager.TranslateContains(paramName))
                                    {
                                        if (TranslationManager.DeleteCustomTranslation(paramName, data.ChannelID, lang, data.Platform))
                                        {
                                            commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "command:custom_translation:delete", "", data.Platform), "key", paramName));
                                            TranslationManager.UpdateTranslation(lang, data.ChannelID, data.Platform);
                                        }
                                        else
                                        {
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:translation_delete", "", data.Platform));
                                            commandReturn.SetColor(ChatColorPresets.Red);
                                        }
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:translation_key_is_not_exist", "", data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.GoldenRod);
                                    }
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(Text.ArgumentsReplacement(TranslationManager.GetTranslation(data.User.Language, "error:translation_lang_is_not_exist", "", data.Platform),
                                        new(){
                                            { "lang", lang },
                                            { "langs", string.Join(", ", langs) }
                                       }));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        catch (Exception ex)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:unknown", data.Channel, data.Platform));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", "", data.Platform), "command_example", "#ct set [paramName] [en/ru] [text]"));
                        commandReturn.SetColor(ChatColorPresets.Red);
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
