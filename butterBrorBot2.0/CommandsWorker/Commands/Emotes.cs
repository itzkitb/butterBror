using static butterBror.BotWorker.FileMng;
using static butterBror.BotWorker;
using butterBib;
using System.Drawing;

namespace butterBror
{
    public partial class Commands
    {
        public class Emotes
        {
            public static CommandInfo Info = new()
            {
                Name = "Emotes",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "При помощи этой команды вы можете посмотреть количество 7tv/bttv/ffz на канале или вывести рандомный эмоут.",
                UseURL = "https://itzkitb.ru/bot_command/emote",
                UserCooldown = 5,
                GlobalCooldown = 2,
                aliases = ["emote", "emotes", "эмоут", "эмоуты"],
                ArgsRequired = "(update/random (7tv/ffz/bttv))",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static async Task<CommandReturn> IndexAsync(CommandData data)
            {
                string[] updateAlias = ["update", "обновить", "u", "о"];
                string[] randomAlias = ["random", "рандом", "рандомный", "r", "р"];

                string resultMessage = "";
                if (data.args.Count > 0)
                {
                    if (updateAlias.Contains(data.args.ElementAt(0)))
                    {
                        if (UsersData.UserGetData<bool>(data.UserUUID, "isBotDev") || UsersData.UserGetData<bool>(data.UserUUID, "isBotModerator") || (bool)data.User.IsChannelAdmin || (bool)data.User.IsChannelBroadcaster)
                        {
                            await Tools.EmoteUpdate(data.Channel);
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "emotesUpdated", data.ChannelID)
                                .Replace("%ffzEmotes%", Bot.EmotesByChannel[data.Channel + "ffz"].Count().ToString())
                                .Replace("%7tvEmotes%", Bot.EmotesByChannel[data.Channel + "7tv"].Count().ToString())
                                .Replace("%bttvEmotes%", Bot.EmotesByChannel[data.Channel + "bttv"].Count().ToString());
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noAccess", data.ChannelID);
                        }
                    }
                    else if (randomAlias.Contains(data.args.ElementAt(0)))
                    {
                        if (data.args.Count > 1)
                        {
                            if (!(Bot.EmotesByChannel.ContainsKey(data.Channel + "7tv") || Bot.EmotesByChannel.ContainsKey(data.Channel + "ffz") || Bot.EmotesByChannel.ContainsKey(data.Channel + "bttv")))
                            {
                                await Tools.EmoteUpdate(data.Channel);
                            }
                            string[] services = ["7tv", "bttv", "ffz"];
                            string selectedService = data.args.ElementAt(1).ToLower();
                            if (services.Contains(selectedService))
                            {
                                var randomEmote = Tools.RandomEmote(data.Channel, selectedService);
                                if (randomEmote["status"] == "OK")
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "randomTypeEmote", data.ChannelID)
                                        .Replace("%emote%", randomEmote["emote"])
                                        .Replace("%serviceType%", selectedService);
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noTypeEmotes", data.ChannelID)
                                        .Replace("%serviceType%", selectedService);
                                }
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID)
                                    .Replace("%commandWorks%", "#emotes random 7tv/bttv/ffz");
                            }
                        }
                        else
                        {
                            if (!(Bot.EmotesByChannel.ContainsKey(data.Channel + "7tv") || Bot.EmotesByChannel.ContainsKey(data.Channel + "ffz") || Bot.EmotesByChannel.ContainsKey(data.Channel + "bttv")))
                            {
                                await Tools.EmoteUpdate(data.Channel);
                            }
                            bool isCompleted = false;
                            int attempts = 0;
                            Random rand = new();
                            string[] services = ["7tv", "bttv", "ffz"];
                            while (attempts <= 100 && !isCompleted)
                            {
                                attempts++;
                                var service = services[rand.Next(services.Length)];
                                var randomEmote = Tools.RandomEmote(data.Channel, service);
                                if (randomEmote["status"] == "OK")
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "randomTypeEmote", data.ChannelID)
                                        .Replace("%emote%", randomEmote["emote"])
                                        .Replace("%serviceType%", service);
                                    isCompleted = true;
                                }
                            }
                            if (!isCompleted)
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noEmotes", data.ChannelID);
                            }
                        }
                    }
                }
                else
                {
                    if (Bot.EmotesByChannel.ContainsKey(data.Channel + "7tv") || Bot.EmotesByChannel.ContainsKey(data.Channel + "ffz") || Bot.EmotesByChannel.ContainsKey(data.Channel + "bttv"))
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "emotesCount", data.ChannelID)
                            .Replace("%ffzEmotes%", Bot.EmotesByChannel[data.Channel + "ffz"].Count().ToString())
                            .Replace("%7tvEmotes%", Bot.EmotesByChannel[data.Channel + "7tv"].Count().ToString())
                            .Replace("%bttvEmotes%", Bot.EmotesByChannel[data.Channel + "bttv"].Count().ToString());
                    }
                    else
                    {
                        await Tools.EmoteUpdate(data.Channel);
                        if (Bot.EmotesByChannel.ContainsKey(data.Channel + "7tv") || Bot.EmotesByChannel.ContainsKey(data.Channel + "ffz") || Bot.EmotesByChannel.ContainsKey(data.Channel + "bttv"))
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "emotesCount", data.ChannelID)
                                .Replace("%ffzEmotes%", Bot.EmotesByChannel[data.Channel + "ffz"].Count().ToString())
                                .Replace("%7tvEmotes%", Bot.EmotesByChannel[data.Channel + "7tv"].Count().ToString())
                                .Replace("%bttvEmotes%", Bot.EmotesByChannel[data.Channel + "bttv"].Count().ToString());
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noEmotes", data.ChannelID);
                        }
                    }
                }
                return new()
                {
                    Message = resultMessage,
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = true,
                    Ephemeral = false,
                    Title = "",
                    Color = (Discord.Color)Color.Green,
                    NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.Coral
                };
            }
        }
    }
}
