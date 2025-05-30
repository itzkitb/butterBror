using butterBror.Utils;
using butterBror.Utils.DataManagers;
using DankDB;
using SevenTV.Types;
using System.Drawing;
using TwitchLib.Client.Enums;
using V8.Net;
using static butterBror.Utils.Command;

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
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string[] updateAlias = ["update", "обновить", "u", "о"];
                    string[] randomAlias = ["random", "рандом", "рандомный", "r", "р"];
                    string[] addAlias = ["add", "добавить", "д", "a"];
                    string[] deleteAlias = ["remove", "delete", "удалить", "у", "d"];
                    string[] renameAlias = ["rename", "переименовать", "ren", "re", "п"];

                    if (data.arguments.Count > 0)
                    {
                        if (updateAlias.Contains(GetArgument(data.arguments, 0)))
                        {
                            if (UsersData.Get<bool>(data.user_id, "isBotDev", data.platform) || UsersData.Get<bool>(data.user_id, "isBotModerator", data.platform) || (bool)data.user.channel_moderator || (bool)data.user.channel_broadcaster)
                            {
                                await Utils.Emotes.EmoteUpdate(data.channel, data.channel_id);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:updated", data.channel_id, data.platform)
                                    .Replace("%emotes%", Maintenance.channels_7tv_emotes[data.channel_id].emotes.Count().ToString()));
                            }
                            else
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:not_enough_rights", data.channel_id, data.platform));
                        }
                        else if (randomAlias.Contains(GetArgument(data.arguments, 0)))
                        {
                            if (!Maintenance.channels_7tv_emotes.ContainsKey(data.channel_id)) await Utils.Emotes.EmoteUpdate(data.channel, data.channel_id);
                            var randomEmote = Utils.Emotes.RandomEmote(data.channel, data.channel_id);
                            if (randomEmote != null)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:random", data.channel_id, data.platform)
                                    .Replace("%emote%", randomEmote.Result));
                            else
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:empty", data.channel_id, data.platform));
                        }
                        else if ((bool)data.user.bot_moderator || (bool)data.user.channel_moderator || (bool)data.user.channel_broadcaster || (bool)data.user.bot_developer)
                        {
                            if (addAlias.Contains(GetArgument(data.arguments, 0)))
                            {
                                commandReturn.SetMessage(await ProcessAddEmote(data));
                            }
                            else if (deleteAlias.Contains(GetArgument(data.arguments, 0)))
                            {
                                commandReturn.SetMessage(await ProcessDeleteEmote(data));
                            }
                            else if (renameAlias.Contains(GetArgument(data.arguments, 0)))
                            {
                                commandReturn.SetMessage(await ProcessRenameEmote(data));
                            }
                        }
                        else if (addAlias.Contains(GetArgument(data.arguments, 0)) || deleteAlias.Contains(GetArgument(data.arguments, 0)) || renameAlias.Contains(GetArgument(data.arguments, 0)))
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:not_enough_rights", data.channel_id, data.platform));
                        }
                    }
                    else
                    {
                        if (Maintenance.channels_7tv_emotes.ContainsKey(data.channel_id))
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:info", data.channel_id, data.platform)
                                .Replace("%emotes%", Maintenance.channels_7tv_emotes[data.channel_id].emotes.Count().ToString()));
                        else
                        {
                            await Utils.Emotes.EmoteUpdate(data.channel, data.channel_id);
                            if (Maintenance.channels_7tv_emotes[data.channel_id].emotes.Count > 0)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:info", data.channel_id, data.platform)
                                    .Replace("%emotes%", Maintenance.channels_7tv_emotes[data.channel_id].emotes.Count().ToString()));
                            else
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:empty", data.channel_id, data.platform));
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

                if (Maintenance.users_7tv_ids is not null && Maintenance.users_7tv_ids.ContainsKey(id))
                    from_id = Maintenance.users_7tv_ids[id];
                else
                {
                    from_id = Maintenance.sevenTvService.SearchUser(Names.GetUsername(id, Platforms.Twitch), Maintenance.token_7tv).Result;
                    if (from_id != null)
                    {
                        if (Maintenance.users_7tv_ids is null)
                            Maintenance.users_7tv_ids = new();

                        Maintenance.users_7tv_ids.Add(id, from_id);
                        Manager.Save(Maintenance.path_7tv_cache, "Ids", Maintenance.users_7tv_ids);
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
                if (GetArgument(data.arguments, 1).IsNullOrWhiteSpace())
                {
                    return ShowLowArgsError(data, "#emote add emotename as:alias from:itzkitb", data.platform);
                }

                var (setId, error) = await GetEmoteSetId(data.channel_id, data.platform);
                if (setId == null)
                {
                    return error;
                }

                string from = GetArgument(data.arguments, "from");
                string emoteName = GetArgument(data.arguments, "as") ?? GetArgument(data.arguments, 1);

                if (FindEmoteInSet(setId, emoteName).Result is not null)
                {
                    return TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:add:already", data.channel_id, data.platform);
                }
                else if (string.IsNullOrWhiteSpace(from))
                {
                    return await AddEmoteFromGlobal(setId, emoteName, data, data.platform);
                }
                else
                {
                    return await AddEmoteFromUser(setId, from, emoteName, data, data.platform);
                }
            }

            public async Task<string> ProcessDeleteEmote(CommandData data)
            {
                string emoteName = GetArgument(data.arguments, 1);
                if (string.IsNullOrWhiteSpace(emoteName))
                {
                    return ShowLowArgsError(data, "#emote delete emotename", data.platform);
                }

                var (setId, error) = await GetEmoteSetId(data.channel_id, data.platform);
                if (setId == null)
                {
                    return error;
                }

                var emote = await FindEmoteInSet(setId, emoteName);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:not_founded", data.channel_id, data.platform)
                        .Replace("%emote%", emoteName);
                }

                var result = await Maintenance.sevenTvService.Remove(setId, emote.id, Maintenance.token_7tv);
                return ProcessResult(result, data, "command:emotes:7tv:removed", "command:emotes:7tv:noaccess:editor", emoteName, data.platform);
            }

            public async Task<string> ProcessRenameEmote(CommandData data)
            {
                string oldName = GetArgument(data.arguments, 1);
                string newName = GetArgument(data.arguments, "as");

                if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                {
                    return ShowLowArgsError(data, "#emote rename oldname as:newname", data.platform);
                }

                var (setId, error) = await GetEmoteSetId(data.channel_id, data.platform);
                if (setId == null)
                {
                    return error;
                }

                var emote = await FindEmoteInSet(setId, oldName);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:not_founded", data.channel_id, data.platform)
                        .Replace("%emote%", oldName);
                }

                var result = await Maintenance.sevenTvService.Rename(setId, newName, emote.id, Maintenance.token_7tv);
                return ProcessResult(result, data, "command:emotes:7tv:renamed", "command:emotes:7tv:noaccess:editor", $"{oldName} → {newName}", data.platform);
            }
            #endregion
            #region Help Methods
            public async Task<(string setId, string error)> GetEmoteSetId(string channelId, Platforms platform)
            {
                if (Maintenance.emoteSetCache.TryGetValue(channelId, out var cached) &&
                    DateTime.UtcNow < cached.expiration)
                {
                    return (cached.setId, null);
                }

                try
                {
                    var userId = GetUserID(channelId);
                    var user = await Maintenance.sevenTv.GetUser(userId);
                    var setId = user.connections[0].emote_set.id;

                    Maintenance.emoteSetCache[channelId] = (setId, DateTime.UtcNow.Add(Maintenance.CacheTTL));
                    return (setId, null);
                }
                catch (Exception ex)
                {
                    Utils.Console.WriteError(ex, $"Emotes\\GetEmoteSetId#{channelId}");
                    return (null, TranslationManager.GetTranslation("en", "command:emotes:7tv:set_error", channelId, platform));
                }
            }

            public async Task<Emote> FindEmoteInSet(string setId, string emoteName)
            {
                var cacheKey = $"{setId}_{emoteName}";
                if (Maintenance.emoteCache.TryGetValue(cacheKey, out var cached) &&
                    DateTime.UtcNow < cached.expiration)
                {
                    return cached.emote;
                }

                var set = await Maintenance.sevenTv.GetEmoteSet(setId);
                var emote = set.emotes.FirstOrDefault(e => e.name.Equals(emoteName));

                if (emote != null)
                {
                    Maintenance.emoteCache[cacheKey] = (emote, DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));
                }

                return emote;
            }
            #endregion
            #region Other Things
            public string ProcessResult(bool result, CommandData data, string successKey, string errorKey, string emoteName, Platforms platform)
            {
                return result
                    ? TranslationManager.GetTranslation(data.user.language, successKey, data.channel_id, platform).Replace("%emote%", emoteName)
                    : TranslationManager.GetTranslation(data.user.language, errorKey, data.channel_id, platform);
            }

            public string ShowLowArgsError(CommandData data, string commandExample, Platforms platform)
            {
                return TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", data.channel_id, platform)
                    .Replace("command_example", commandExample);
            }

            public async Task<string> AddEmoteFromGlobal(string setId, string emoteName, CommandData data, Platforms platform)
            {
                var emote = await Maintenance.sevenTvService.SearchEmote(emoteName, Maintenance.token_7tv);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:not_founded", data.channel_id, platform)
                        .Replace("%emote%", emoteName);
                }

                var result = await Maintenance.sevenTvService.Add(setId, emoteName, emote, Maintenance.token_7tv);
                return ProcessResult(result, data, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, platform);
            }

            public async Task<string> AddEmoteFromUser(string setId, string fromUser, string emoteName, CommandData data, Platforms platform)
            {
                var sourceUserId = GetUserID(Names.GetUserID(fromUser, Platforms.Twitch));
                var sourceUser = await Maintenance.sevenTv.GetUser(sourceUserId);
                var sourceSetId = sourceUser.connections[0].emote_set.id;

                var emote = await FindEmoteInSet(sourceSetId, emoteName);
                if (emote == null)
                {
                    return TranslationManager.GetTranslation(data.user.language, "command:emotes:7tv:not_founded", data.channel_id, platform)
                        .Replace("%emote%", emoteName);
                }

                var result = await Maintenance.sevenTvService.Add(setId, emoteName, emote.id, Maintenance.token_7tv);
                return ProcessResult(result, data, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, platform);
            }
            #endregion
        }
    }
}
