using butterBror.Core.Bot;
using butterBror.Models;
using butterBror.Utils;
using DankDB;
using SevenTV.Types.Rest;
using V8.Net;
using static butterBror.Core.Bot.Console;
using static butterBror.Utils.MessageProcessor;

namespace butterBror.Core.Commands.List
{
    public class Emotes : CommandBase
    {
        public override string Name => "Emotes";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Emotes.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Работа с 7tv эмоутами." },
            { "en-US", "Working with 7tv emotes." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=emote";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 2;
        public override string[] Aliases => ["emote", "emotes", "эмоут", "эмоуты", "7tv", "seventv", "семьтелефизоров", "7тв"];
        public override string HelpArguments => "(update/random/add {name} (as:{name}, from:{channel})/delete {name}/rename {old_name} {new_name})";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
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
                        if (butterBror.Bot.SQL.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.ID)) || butterBror.Bot.SQL.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.ID)) || (bool)data.User.IsModerator || (bool)data.User.IsBroadcaster)
                        {
                            await Utils.SevenTvEmoteCache.EmoteUpdate(data.Channel, data.ChannelId);
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:emotes:7tv:updated",
                                data.ChannelId,
                                data.Platform,
                                butterBror.Bot.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count().ToString()
                                ));
                        }
                        else
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_rights", data.ChannelId, data.Platform));
                    }
                    else if (randomAlias.Contains(GetArgument(data.Arguments, 0)))
                    {
                        if (!butterBror.Bot.ChannelsSevenTVEmotes.ContainsKey(data.ChannelId)) await Utils.SevenTvEmoteCache.EmoteUpdate(data.Channel, data.ChannelId);
                        var randomEmote = Utils.SevenTvEmoteCache.RandomEmote(data.Channel, data.ChannelId);
                        if (randomEmote != null)
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:emotes:7tv:random",
                                data.ChannelId,
                                data.Platform,
                                randomEmote.Result
                                ));
                        else
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:emotes:7tv:empty", data.ChannelId, data.Platform));
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
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_rights", data.ChannelId, data.Platform));
                    }
                }
                else
                {
                    if (butterBror.Bot.ChannelsSevenTVEmotes.ContainsKey(data.ChannelId))
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:emotes:7tv:info",
                            data.ChannelId,
                            data.Platform,
                            butterBror.Bot.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count().ToString()
                            ));
                    else
                    {
                        await Utils.SevenTvEmoteCache.EmoteUpdate(data.Channel, data.ChannelId);
                        if (butterBror.Bot.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count > 0)
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:emotes:7tv:info",
                                data.ChannelId,
                                data.Platform,
                                butterBror.Bot.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count().ToString()
                                ));
                        else
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:emotes:7tv:empty", data.ChannelId, data.Platform));
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

            if (butterBror.Bot.UsersSevenTVIDs is not null && butterBror.Bot.UsersSevenTVIDs.ContainsKey(id))
                from_id = butterBror.Bot.UsersSevenTVIDs[id];
            else
            {
                from_id = butterBror.Bot.SevenTvService.SearchUser(UsernameResolver.GetUsername(id, PlatformsEnum.Twitch, true), butterBror.Bot.Tokens.SevenTV).Result;
                if (from_id != null)
                {
                    if (butterBror.Bot.UsersSevenTVIDs is null)
                        butterBror.Bot.UsersSevenTVIDs = new();

                    butterBror.Bot.UsersSevenTVIDs.Add(id, from_id);
                    Manager.Save(butterBror.Bot.Paths.SevenTVCache, "Ids", butterBror.Bot.UsersSevenTVIDs);
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

            var (setId, error) = await GetEmoteSetId(data.ChannelId, data.Platform);
            if (setId == null)
            {
                return error;
            }

            string from = GetArgument(data.Arguments, "from");
            string emoteName = GetArgument(data.Arguments, "as") ?? GetArgument(data.Arguments, 1);

            if (FindEmoteInSet(setId, emoteName).Result is not null)
            {
                return LocalizationService.GetString(data.User.Language, "command:emotes:7tv:add:already", data.ChannelId, data.Platform);
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

            var (setId, error) = await GetEmoteSetId(data.ChannelId, data.Platform);
            if (setId == null)
            {
                return error;
            }

            var emote = await FindEmoteInSet(setId, emoteName);
            if (emote == null)
            {
                return LocalizationService.GetString(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelId, data.Platform, emoteName);
            }

            var result = await butterBror.Bot.SevenTvService.Remove(setId, emote.id, butterBror.Bot.Tokens.SevenTV);
            if (result && butterBror.Bot.EmotesCache.ContainsKey($"{setId}_{emoteName}"))
            {
                butterBror.Bot.EmotesCache.Remove($"{setId}_{emoteName}", out var cache);
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

            var (setId, error) = await GetEmoteSetId(data.ChannelId, data.Platform);
            if (setId == null)
            {
                return error;
            }

            var emote = await FindEmoteInSet(setId, oldName);
            if (emote == null)
            {
                return LocalizationService.GetString(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelId, data.Platform, oldName);
            }

            var result = await butterBror.Bot.SevenTvService.Rename(setId, newName, emote.id, butterBror.Bot.Tokens.SevenTV);
            return ProcessResult(result, data, "command:emotes:7tv:renamed", "command:emotes:7tv:noaccess:editor", $"{oldName} → {newName}", data.Platform);
        }
        #endregion
        #region Help Methods

        public async Task<(string setId, string error)> GetEmoteSetId(string channelId, PlatformsEnum platform)
        {
            if (butterBror.Bot.EmoteSetsCache.TryGetValue(channelId, out var cached) &&
                DateTime.UtcNow < cached.expiration)
            {
                return (cached.setId, null);
            }

            try
            {
                var userId = GetUserID(channelId);
                var user = await butterBror.Bot.Clients.SevenTV.rest.GetUser(userId);
                var setId = user.connections[0].emote_set.id;

                butterBror.Bot.EmoteSetsCache[channelId] = (setId, DateTime.UtcNow.Add(butterBror.Bot.CacheTTL));
                return (setId, null);
            }
            catch (Exception ex)
            {
                Write(ex);
                return (null, LocalizationService.GetString("en-US", "command:emotes:7tv:set_error", channelId, platform));
            }
        }

        public async Task<Emote> FindEmoteInSet(string setId, string emoteName)
        {
            var cacheKey = $"{setId}_{emoteName}";
            if (butterBror.Bot.EmotesCache.TryGetValue(cacheKey, out var cached) &&
                DateTime.UtcNow < cached.expiration)
            {
                return cached.emote;
            }

            var set = await butterBror.Bot.Clients.SevenTV.rest.GetEmoteSet(setId);
            var emote = set.emotes.FirstOrDefault(e => e.name.Equals(emoteName));

            if (emote != null)
            {
                butterBror.Bot.EmotesCache[cacheKey] = (emote, DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));
            }

            return emote;
        }
        #endregion
        #region Other Things
        public string ProcessResult(bool result, CommandData data, string successKey, string errorKey, string emoteName, PlatformsEnum platform)
        {
            return result
                ? LocalizationService.GetString(data.User.Language, successKey, data.ChannelId, platform, emoteName)
                : LocalizationService.GetString(data.User.Language, errorKey, data.ChannelId, platform);
        }

        public string ShowLowArgsError(CommandData data, string commandExample, PlatformsEnum platform)
        {
            return LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", data.ChannelId, platform)
                .Replace("command_example", commandExample);
        }

        public async Task<string> AddEmoteFromGlobal(string setId, string emoteName, CommandData data, PlatformsEnum platform)
        {
            var emote = await butterBror.Bot.SevenTvService.SearchEmote(emoteName, butterBror.Bot.Tokens.SevenTV);
            if (emote == null)
            {
                return LocalizationService.GetString(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelId, platform, emoteName);
            }

            var result = await butterBror.Bot.SevenTvService.Add(setId, emoteName, emote, butterBror.Bot.Tokens.SevenTV);
            return ProcessResult(result, data, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, platform);
        }

        public async Task<string> AddEmoteFromUser(string setId, string fromUser, string emoteName, CommandData data, PlatformsEnum platform)
        {
            var sourceUserId = GetUserID(UsernameResolver.GetUserID(fromUser, PlatformsEnum.Twitch));
            var sourceUser = await butterBror.Bot.Clients.SevenTV.rest.GetUser(sourceUserId);
            var sourceSetId = sourceUser.connections[0].emote_set.id;

            var emote = await FindEmoteInSet(sourceSetId, emoteName);
            if (emote == null)
            {
                return LocalizationService.GetString(data.User.Language, "command:emotes:7tv:not_founded", data.ChannelId, platform, emoteName);
            }

            var result = await butterBror.Bot.SevenTvService.Add(setId, emoteName, emote.id, butterBror.Bot.Tokens.SevenTV);
            return ProcessResult(result, data, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, platform);
        }
        #endregion
    }
}
