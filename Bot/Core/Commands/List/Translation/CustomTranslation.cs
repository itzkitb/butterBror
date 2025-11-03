using bb.Utils;
using bb.Core.Configuration;
using TwitchLib.Client.Enums;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Translation
{
    public class CustomTranslation : CommandBase
    {
        public override string Name => "CustomTranslation";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Translation/CustomTranslation.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Заменить стандартные сообщения собственными." },
            { Language.EnUs, "Replace the default messages with your own." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 5;
        public override string[] Aliases => ["customtranslation", "ct", "кастомныйперевод", "кп"];
        public override string Help => "set <param> <en|ru> <text> | get <param> <en|ru> | original <param> <en|ru> | reset <param> <en|ru>";
        public override DateTime CreationDate => DateTime.Parse("2024-05-08T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.ChatMod;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.Channel == null || data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:API_error", string.Empty, data.Platform));
                    return commandReturn;
                }

                string[] setAlias = ["set", "s", "установить", "сет", "с", "у"];
                string[] getAlias = ["get", "g", "гет", "получить", "п", "г"];
                string[] origAlias = ["original", "оригинал", "о", "o"];
                string[] delAlias = ["delete", "del", "d", "remove", "reset", "сбросить", "удалить", "с"];

                Dictionary<Language, string[]> languagesDictionary = new(){
                    { Language.EnUs, ["en", "en-us", "us"] },
                    { Language.RuRu, ["ru", "ru-ru"] }
                };

                string[] uneditableItems = ["text:bot_info", "cantSend", "lowArgs", "lang", "wrongArgs", "changedLang", "commandDoesntWork", "noneExistUser", "noAccess", "userBanned",
                    "userPardon", "rejoinedChannel", "joinedChannel", "leavedChannel", "modAdded", "modDel", "addedChannel", "delChannel", "welcomeChannel", "error", "botVerified",
                    "unhandledError", "Err"];

                if (data.Arguments != null && data.Arguments.Count >= 3)
                {
                    try
                    {
                        string argumentOne = data.Arguments[0];
                        string languageParameterName = data.Arguments[1];
                        string stringLanguage = data.Arguments[2];

                        Language? prepLang = null;

                        foreach (KeyValuePair<Language, string[]> l in languagesDictionary)
                        {
                            if (l.Value.Contains(stringLanguage))
                            {
                                prepLang = l.Key;
                            }
                        }

                        if (prepLang != null)
                        {
                            Language lang = (Language)prepLang;
                            if (setAlias.Contains(argumentOne))
                            {
                                if (data.Arguments.Count > 3)
                                {
                                    List<string> argumentsList = data.Arguments;
                                    argumentsList = argumentsList.Skip(3).ToList();
                                    string translate = string.Join(' ', argumentsList);

                                    if (uneditableItems.Contains(languageParameterName))
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:translation_secured", "", data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                    else
                                    {
                                        if (LocalizationService.TranslateContains(languageParameterName))
                                        {
                                            if (LocalizationService.SetCustomTranslation(languageParameterName, translate, data.ChannelId, lang, data.Platform))
                                            {
                                                LocalizationService.UpdateTranslation(lang, data.ChannelId, data.Platform);
                                                commandReturn.SetMessage(LocalizationService.GetString(
                                                    data.User.Language,
                                                    "command:custom_translation:set",
                                                    string.Empty,
                                                    data.Platform,
                                                    languageParameterName));
                                            }
                                            else
                                            {
                                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:translation_set", "", data.Platform));
                                                commandReturn.SetColor(ChatColorPresets.Red);
                                            }
                                        }
                                        else
                                        {
                                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:translation_key_is_not_exist", "", data.Platform));
                                            commandReturn.SetColor(ChatColorPresets.GoldenRod);
                                        }
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(
                                        data.User.Language,
                                        "error:not_enough_arguments",
                                        string.Empty,
                                        data.Platform,
                                        $"{Program.BotInstance.DefaultCommandPrefix}ct set [paramName] [en/ru] [text]"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else if (getAlias.Contains(argumentOne))
                            {
                                if (LocalizationService.TranslateContains(languageParameterName))
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(
                                        data.User.Language,
                                        "command:custom_translation:get",
                                        string.Empty,
                                        data.Platform,
                                        languageParameterName,
                                        LocalizationService.GetString(lang, languageParameterName, data.ChannelId, data.Platform)));
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:translation_key_is_not_exist", "", data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.GoldenRod);
                                }
                            }
                            else if (origAlias.Contains(argumentOne))
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:custom_translation:original",
                                    string.Empty,
                                    data.Platform,
                                    languageParameterName,
                                    LocalizationService.GetString(lang, languageParameterName, string.Empty, data.Platform)));
                            }
                            else if (delAlias.Contains(argumentOne) && data.User.Roles >= Roles.ChatMod)
                            {
                                if (LocalizationService.TranslateContains(languageParameterName))
                                {
                                    if (LocalizationService.DeleteCustomTranslation(languageParameterName, data.ChannelId, lang, data.Platform))
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(
                                            data.User.Language,
                                            "command:custom_translation:delete",
                                            string.Empty,
                                            data.Platform,
                                            languageParameterName));
                                        LocalizationService.UpdateTranslation(lang, data.ChannelId, data.Platform);
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:translation_delete", "", data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:translation_key_is_not_exist", "", data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.GoldenRod);
                                }
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "error:translation_lang_is_not_exist",
                                string.Empty,
                                data.Platform,
                                stringLanguage,
                                string.Join(", ", languagesDictionary)));

                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    catch (Exception)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", data.Channel, data.Platform));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "error:not_enough_arguments",
                        string.Empty,
                        data.Platform,
                        $"{Program.BotInstance.DefaultCommandPrefix}ct set [paramName] [en/ru] [text]"));
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