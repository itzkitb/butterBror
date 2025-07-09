using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;
using DankDB;
using SevenTV.Types;
using SevenTV.Types.Rest;
using System.Drawing;
using TwitchLib.Client.Enums;
using V8.Net;
using static butterBror.Commands;
using static butterBror.Utils.Bot.Console;
using static butterBror.Utils.Tools.Command;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Работа с 7tv эмоутами" },
                    { "en", "Working with 7tv emotes" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=emote",
                CooldownPerUser = 5,
                CooldownPerChannel = 2,
                Aliases = ["emote", "emotes", "эмоут", "эмоуты", "7tv", "seventv", "семьтелефизоров", "7тв"],
                Arguments = "(update/random/add {name} (as:{name}, from:{channel})/delete {name}/rename {old_name} {new_name})",
                CooldownReset = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch]
            };
            public async Task<CommandReturn> Index(CommandData data)
            {
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string[] updateAlias = ["update", "обновить", "u", "о"];
                    string[] randomAlias = ["random", "рандом", "рандомный", "r", "р"];
                    string[] addAlias = ["add", "добавить", "д", "a"];
                    string[] deleteAlias = ["remove", "delete", "удалить", "у", "d"];
                    string[] renameAlias = ["rename", "переименовать", "ren", "re", "п"];

                    if (data.Arguments.Count > 0)
                    {
                        if (updateAlias.Contains(GetArgument(data.Arguments, 0)))
                        {
                            if (UsersData.Get<bool>(data.UserID, "isBotDev", data.Platform) || UsersData.Get<bool>(data.UserID, "isBotModerator", data.Platform) || (bool)data.User.IsModerator || (bool)data.User.IsBroadcaster)
                            {
                                await Utils.Tools.Emotes.EmoteUpdate(data.Channel, data.ChannelID);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:updated", data.ChannelID, data.Platform)
                                    .Replace("%emotes%", Engine.Bot.ChannelsSevenTVEmotes[data.ChannelID].emotes.Count().ToString()));
                            }
                            else
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_rights", data.ChannelID, data.Platform));
                        }
                        else if (randomAlias.Contains(GetArgument(data.Arguments, 0)))
                        {
                            if (!Engine.Bot.ChannelsSevenTVEmotes.ContainsKey(data.ChannelID)) await Utils.Tools.Emotes.EmoteUpdate(data.Channel, data.ChannelID);
                            var randomEmote = Utils.Tools.Emotes.RandomEmote(data.Channel, data.ChannelID);
                            if (randomEmote != null)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:random", data.ChannelID, data.Platform)
                                    .Replace("%emote%", randomEmote.Result));
                            else
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:empty", data.ChannelID, data.Platform));
                        }
                        else if ((bool)data.User.IsBotModerator || (bool)data.User.IsModerator || (bool)data.User.IsBroadcaster || (bool)data.User.IsBotDeveloper)
                        {
                            if (addAlias.Contains(GetArgument(data.Arguments, 0)))
                            {
                                commandReturn.SetMessage(await ProcessAddEmote(data));
                            }
                            else if (deleteAlias.Contains(GetArgument(data.Arguments, 0)))
                            {
                                commandReturn.SetMessage(await ProcessDeleteEmote(data));
                            }
                            else if (renameAlias.Contains(GetArgument(data.Arguments, 0)))
                            {
                                commandReturn.SetMessage(await ProcessRenameEmote(data));
                            }
                        }
                        else if (addAlias.Contains(GetArgument(data.Arguments, 0)) || deleteAlias.Contains(GetArgument(data.Arguments, 0)) || renameAlias.Contains(GetArgument(data.Arguments, 0)))
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_rights", data.ChannelID, data.Platform));
                        }
                    }
                    else
                    {
                        if (Engine.Bot.ChannelsSevenTVEmotes.ContainsKey(data.ChannelID))
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:info", data.ChannelID, data.Platform)
                                .Replace("%emotes%", Engine.Bot.ChannelsSevenTVEmotes[data.ChannelID].emotes.Count().ToString()));
                        else
                        {
                            await Utils.Tools.Emotes.EmoteUpdate(data.Channel, data.ChannelID);
                            if (Engine.Bot.ChannelsSevenTVEmotes[data.ChannelID].emotes.Count > 0)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:info", data.ChannelID, data.Platform)
                                    .Replace("%emotes%", Engine.Bot.ChannelsSevenTVEmotes[data.ChannelID].emotes.Count().ToString()));
                            else
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:empty", data.ChannelID, data.Platform));
                        }
                    }
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }

            #region 7tv Is Shit
            public string GetUserID(string id)
            {
                string from_id = null;

                if (Engine.Bot.UsersSevenTVIDs is not null && Engine.Bot.UsersSevenTVIDs.ContainsKey(id))
                    from_id = Engine.Bot.UsersSevenTVIDs[id];
                else
                {
                    from_id = Engine.Bot.SevenTvService.SearchUser(Names.GetUsername(id, Platforms.Twitch), Engine.Bot.Tokens.SevenTV).Result;
                    if (from_id != null)
                    {
                        if (Engine.Bot.UsersSevenTVIDs is null)
                            Engine.Bot.UsersSevenTVIDs = new();

                        Engine.Bot.UsersSevenTVIDs.Add(id, from_id);
                        SafeManager.Save(Engine.Bot.Pathes.SevenTVCache, "Ids", Engine.Bot.UsersSevenTVIDs);
                    }
                }

                return from_id;
            }

            public string GetEmoteFromList(Emote[] list, string name)
            {
                foreach (Emote e in list)
                    if (e.name == name)
                        return e.id;
                return null;
            }
            #endregion
            #region Processes
            public async Task<string> ProcessAddEmote(CommandData data)
            {
                if (GetArgument(data.Arguments, 1).IsNullOrWhiteSpace())
                {
                    return ShowLowArgsError(data, "#emote add emotename as:alias from:itzkitb", data.Platform);
                }

                var (setId, error) = await GetEmoteSetId(data.ChannelID, data.Platform);
                if (setId == null)
                {
                    return error;
                }

                string from = GetArgument(data.Arguments, "from");
                string emoteName = GetArgument(data.Arguments, "as") ?? GetArgument(data.Arguments, 1);

                if (FindEmoteInSet(setId, emoteName).Result is not null)
                {
                    return TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:add:already", data.ChannelID, data.Platform);
                }
                else if (string.IsNullOrWhiteSpace(from))
                {
                    return await AddEmoteFromGlobal(setId, emoteName, data, data.Platform);
                }
                else
                {
                    return await AddEmoteFromUser(setId, from, emoteName, data, data.Platform);
                }
            }

            public async Task<string> ProcessDeleteEmote(CommandData data)
            {
                string emoteName = GetArgument(data.Arguments, 1);
                if (string.IsNullOrWhiteSpace(emoteName))
                {
                    return ShowLowArgsError(data, "#emote delete emotename", data.Platform);
                }

                var (setId, error) = await GetEmoteSetId(data.ChannelID, data.Platform);
                if (setId == null)
                {
                    return error;
                }

                var emote = await FindEmoteInSet(setId, emoteName);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelID, data.Platform)
                        .Replace("%emote%", emoteName);
                }

                var result = await Engine.Bot.SevenTvService.Remove(setId, emote.id, Engine.Bot.Tokens.SevenTV);
                if (result && Engine.Bot.EmotesCache.ContainsKey($"{setId}_{emoteName}"))
                {
                    Engine.Bot.EmotesCache.Remove($"{setId}_{emoteName}", out var cache);
                }

                return ProcessResult(result, data, "command:emotes:7tv:removed", "command:emotes:7tv:noaccess:editor", emoteName, data.Platform);
            }

            public async Task<string> ProcessRenameEmote(CommandData data)
            {
                string oldName = GetArgument(data.Arguments, 1);
                string newName = GetArgument(data.Arguments, "as");

                if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                {
                    return ShowLowArgsError(data, "#emote rename oldname as:newname", data.Platform);
                }

                var (setId, error) = await GetEmoteSetId(data.ChannelID, data.Platform);
                if (setId == null)
                {
                    return error;
                }

                var emote = await FindEmoteInSet(setId, oldName);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelID, data.Platform)
                        .Replace("%emote%", oldName);
                }

                var result = await Engine.Bot.SevenTvService.Rename(setId, newName, emote.id, Engine.Bot.Tokens.SevenTV);
                return ProcessResult(result, data, "command:emotes:7tv:renamed", "command:emotes:7tv:noaccess:editor", $"{oldName} → {newName}", data.Platform);
            }
            #endregion
            #region Help Methods
            [ConsoleSector("butterBror.Commands.Emotes", "GetEmoteSetId")]
            public async Task<(string setId, string error)> GetEmoteSetId(string channelId, Platforms platform)
            {
                if (Engine.Bot.EmoteSetsCache.TryGetValue(channelId, out var cached) &&
                    DateTime.UtcNow < cached.expiration)
                {
                    return (cached.setId, null);
                }

                try
                {
                    var userId = GetUserID(channelId);
                    var user = await Engine.Bot.Clients.SevenTV.rest.GetUser(userId);
                    var setId = user.connections[0].emote_set.id;

                    Engine.Bot.EmoteSetsCache[channelId] = (setId, DateTime.UtcNow.Add(Engine.Bot.CacheTTL));
                    return (setId, null);
                }
                catch (Exception ex)
                {
                    Write(ex);
                    return (null, TranslationManager.GetTranslation("en", "command:emotes:7tv:set_error", channelId, platform));
                }
            }

            public async Task<Emote> FindEmoteInSet(string setId, string emoteName)
            {
                var cacheKey = $"{setId}_{emoteName}";
                if (Engine.Bot.EmotesCache.TryGetValue(cacheKey, out var cached) &&
                    DateTime.UtcNow < cached.expiration)
                {
                    return cached.emote;
                }

                var set = await Engine.Bot.Clients.SevenTV.rest.GetEmoteSet(setId);
                var emote = set.emotes.FirstOrDefault(e => e.name.Equals(emoteName));

                if (emote != null)
                {
                    Engine.Bot.EmotesCache[cacheKey] = (emote, DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));
                }

                return emote;
            }
            #endregion
            #region Other Things
            public string ProcessResult(bool result, CommandData data, string successKey, string errorKey, string emoteName, Platforms platform)
            {
                return result
                    ? TranslationManager.GetTranslation(data.User.Language, successKey, data.ChannelID, platform).Replace("%emote%", emoteName)
                    : TranslationManager.GetTranslation(data.User.Language, errorKey, data.ChannelID, platform);
            }

            public string ShowLowArgsError(CommandData data, string commandExample, Platforms platform)
            {
                return TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, platform)
                    .Replace("command_example", commandExample);
            }

            public async Task<string> AddEmoteFromGlobal(string setId, string emoteName, CommandData data, Platforms platform)
            {
                var emote = await Engine.Bot.SevenTvService.SearchEmote(emoteName, Engine.Bot.Tokens.SevenTV);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelID, platform)
                        .Replace("%emote%", emoteName);
                }

                var result = await Engine.Bot.SevenTvService.Add(setId, emoteName, emote, Engine.Bot.Tokens.SevenTV);
                return ProcessResult(result, data, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, platform);
            }

            public async Task<string> AddEmoteFromUser(string setId, string fromUser, string emoteName, CommandData data, Platforms platform)
            {
                var sourceUserId = GetUserID(Names.GetUserID(fromUser, Platforms.Twitch));
                var sourceUser = await Engine.Bot.Clients.SevenTV.rest.GetUser(sourceUserId);
                var sourceSetId = sourceUser.connections[0].emote_set.id;

                var emote = await FindEmoteInSet(sourceSetId, emoteName);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelID, platform)
                        .Replace("%emote%", emoteName);
                }

                var result = await Engine.Bot.SevenTvService.Add(setId, emoteName, emote.id, Engine.Bot.Tokens.SevenTV);
                return ProcessResult(result, data, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, platform);
            }
            #endregion
        }
    }
}
