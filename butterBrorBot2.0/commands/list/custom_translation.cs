using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror;

namespace butterBror
{
    public partial class Commands
    {
        public class CustomTranslation
        {
            public static CommandInfo Info = new()
            {
                name = "CustomTranslation",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Кастомизируй свой канал смешными сообщениями, не будь занудой!" },
                    { "en", "Customize your channel with funny messages, don't be a bore!" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=customtranslation",
                cooldown_per_user = 10,
                cooldown_global = 5,
                aliases = ["CustomTranslation", "ct", "кастомныйперевод", "кп"],
                arguments = "(set [paramName] [en/ru] [text])/(get [paramName] [en/ru])/(original [paramName] [en/ru])/(reset [paramName] [en/ru])",
                cooldown_reset = true,
                creation_date = DateTime.Parse("08/05/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                    string resultMessage = "";
                    string[] setAliases = ["set", "s", "установить", "сет", "с", "у"];
                    string[] getAliases = ["get", "g", "гет", "получить", "п", "г"];
                    string[] originalAliases = ["original", "оригинал", "о", "o"];
                    string[] deleteAliases = ["delete", "del", "d", "remove", "reset", "сбросить", "удалить", "с"];

                    string[] langs = ["ru", "en"];

                    string[] uneditableitems = ["text:bot_info", "cantSend", "lowArgs", "lang", "wrongArgs", "changedLang", "commandDoesntWork", "noneExistUser", "noAccess", "userBanned",
                    "userPardon", "rejoinedChannel", "joinedChannel", "leavedChannel", "modAdded", "modDel", "addedChannel", "delChannel", "welcomeChannel", "error", "botVerified",
                    "unhandledError", "Err"];

                    if (data.arguments.Count >= 3)
                    {
                        try
                        {
                            string arg1 = data.arguments[0];
                            string paramName = data.arguments[1];
                            string lang = data.arguments[2];
                            if (langs.Contains(lang))
                            {
                                if (setAliases.Contains(arg1) && ((bool)data.user.channel_moderator || (bool)data.user.channel_broadcaster || (bool)data.user.bot_moderator))
                                {
                                    if (data.arguments.Count > 3)
                                    {
                                        List<string> textArgs = data.arguments;
                                        textArgs = textArgs.Skip(3).ToList();
                                        string text = string.Join(' ', textArgs);
                                        if (uneditableitems.Contains(paramName))
                                        {
                                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:translation_secured", "", data.platform);
                                            resultNicknameColor = ChatColorPresets.Red;
                                            resultColor = Color.Red;
                                        }
                                        else
                                        {
                                            if (TranslationManager.TranslateContains(paramName))
                                            {
                                                if (TranslationManager.SetCustomTranslation(paramName, text, data.channel_id, lang, data.platform))
                                                {
                                                    TranslationManager.UpdateTranslation(lang, data.channel_id, data.platform);
                                                    resultMessage = TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "command:custom_translation:set", "", data.platform),
                                                        "key", paramName);
                                                }
                                                else
                                                {
                                                    resultMessage = TranslationManager.GetTranslation(data.user.language, "error:translation_set", "", data.platform);
                                                    resultNicknameColor = ChatColorPresets.Red;
                                                    resultColor = Color.Red;
                                                }
                                            }
                                            else
                                            {
                                                resultMessage = TranslationManager.GetTranslation(data.user.language, "error:translation_key_is_not_exist", "", data.platform);
                                                resultNicknameColor = ChatColorPresets.GoldenRod;
                                                resultColor = Color.Gold;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        resultMessage = TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", "", data.platform),
                                            "command_example", "#ct set [paramName] [en/ru] [text]");
                                        resultNicknameColor = ChatColorPresets.Red;
                                        resultColor = Color.Red;
                                    }
                                }
                                else if (getAliases.Contains(arg1) && ((bool)data.user.channel_moderator || (bool)data.user.channel_broadcaster || (bool)data.user.bot_moderator))
                                {
                                    if (TranslationManager.TranslateContains(paramName))
                                    {
                                        resultMessage = TextUtil.ArgumentsReplacement(TranslationManager.GetTranslation(data.user.language, "command:custom_translation:get", "", data.platform),
                                            new(){
                                                { "key", paramName },
                                                { "translation", TranslationManager.GetTranslation(lang, paramName, data.channel_id, data.platform) }
                                            });
                                    }
                                    else
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.user.language, "error:translation_key_is_not_exist", "", data.platform);
                                        resultNicknameColor = ChatColorPresets.GoldenRod;
                                        resultColor = Color.Gold;
                                    }
                                }
                                else if (originalAliases.Contains(arg1))
                                {
                                    resultMessage = TextUtil.ArgumentsReplacement(TranslationManager.GetTranslation(data.user.language, "command:custom_translation:original", "", data.platform),
                                        new(){
                                            { "key", paramName },
                                            { "translation", TranslationManager.GetTranslation(lang, paramName, string.Empty, data.platform) }
                                       });
                                }
                                else if (deleteAliases.Contains(arg1) && ((bool)data.user.channel_moderator || (bool)data.user.channel_broadcaster || (bool)data.user.bot_moderator))
                                {
                                    if (TranslationManager.TranslateContains(paramName))
                                    {
                                        if (TranslationManager.DeleteCustomTranslation(paramName, data.channel_id, lang, data.platform))
                                        {
                                            resultMessage = TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "command:custom_translation:delete", "", data.platform), "key", paramName);
                                            TranslationManager.UpdateTranslation(lang, data.channel_id, data.platform);
                                        }
                                        else
                                        {
                                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:translation_delete", "", data.platform);
                                            resultNicknameColor = ChatColorPresets.Red;
                                            resultColor = Color.Red;
                                        }
                                    }
                                    else
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.user.language, "error:translation_key_is_not_exist", "", data.platform);
                                        resultNicknameColor = ChatColorPresets.GoldenRod;
                                        resultColor = Color.Gold;
                                    }
                                }
                            }
                            else
                            {
                                resultMessage = TextUtil.ArgumentsReplacement(TranslationManager.GetTranslation(data.user.language, "error:translation_lang_is_not_exist", "", data.platform),
                                        new(){
                                            { "lang", lang },
                                            { "langs", string.Join(", ", langs) }
                                       });
                                resultNicknameColor = ChatColorPresets.Red;
                                resultColor = Color.Red;
                            }
                        }
                        catch (Exception ex)
                        {
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:unknown", data.channel, data.platform);
                            resultNicknameColor = ChatColorPresets.Red;
                            resultColor = Color.Red;
                        }
                    }
                    else
                    {
                        resultMessage = TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", "", data.platform), "command_example", "#ct set [paramName] [en/ru] [text]");
                        resultNicknameColor = ChatColorPresets.Red;
                        resultColor = Color.Red;
                    }

                    return new()
                    {
                        message = resultMessage,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = false,
                        is_ephemeral = false,
                        title = "",
                        embed_color = resultColor,
                        nickname_color = resultNicknameColor
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
        }
    }
}
