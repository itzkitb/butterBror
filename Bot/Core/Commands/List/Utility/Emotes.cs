using Acornima;
using bb.Utils;
using bb.Core.Configuration;
using DankDB;
using Jint.Runtime;
using Newtonsoft.Json.Linq;
using SevenTV.Types.Rest;
using static bb.Core.Bot.Logger;
using static bb.Utils.MessageProcessor;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Utility
{
    public class Emotes : CommandBase
    {
        public override string Name => "Emotes";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Utility/Emotes.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Работа с 7tv эмоутами." },
            { Language.EnUs, "Working with 7tv emotes." }
        };
        public override int UserCooldown => 5;
        public override int Cooldown => 1;
        public override string[] Aliases => ["emote", "emotes", "эмоут", "эмоуты", "7tv", "seventv", "семьтелефизоров", "7тв"];
        public override string Help => "update | random | add <name> [as:<name>] [from:<channel>] | delete <name> | rename <old_name> <new_name>";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null || data.Channel == null || Program.BotInstance.DataBase == null ||
                    Program.BotInstance.Tokens.SevenTV == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                string token = Program.BotInstance.Tokens.SevenTV;
                string prefix = Program.BotInstance.DataBase.Channels.GetCommandPrefix(data.Platform, data.ChannelId);

                string[] updateAlias = ["update", "обновить", "u", "о"];
                string[] randomAlias = ["random", "рандом", "рандомный", "r", "р"];
                string[] addAlias = ["add", "добавить", "д", "a"];
                string[] deleteAlias = ["remove", "delete", "удалить", "у", "d"];
                string[] renameAlias = ["rename", "переименовать", "ren", "re", "п"];

                if (data.Arguments != null && data.Arguments.Count > 0)
                {
                    if (updateAlias.Contains(GetArgument(data.Arguments, 0)))
                    {
                        if (data.User.Roles >= Roles.ChatMod)
                        {
                            await SevenTvEmoteCache.EmoteUpdate(data.Channel, data.ChannelId);
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:emotes:7tv:updated",
                                data.ChannelId,
                                data.Platform,
                                Program.BotInstance.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count().ToString()
                                ));
                        }
                        else
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_rights", data.ChannelId, data.Platform));
                    }
                    else if (randomAlias.Contains(GetArgument(data.Arguments, 0)))
                    {
                        if (!Program.BotInstance.ChannelsSevenTVEmotes.ContainsKey(data.ChannelId)) await SevenTvEmoteCache.EmoteUpdate(data.Channel, data.ChannelId);
                        var randomEmote = SevenTvEmoteCache.RandomEmote(data.Channel, data.ChannelId);
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
                    else if (data.User.Roles >= Roles.ChatMod)
                    {
                        if (addAlias.Contains(GetArgument(data.Arguments, 0)))
                        {
                            commandReturn.SetMessage(await ProcessAddEmote(data.Arguments, data.ChannelId, data.Platform, data.User.Language, prefix, token));
                        }
                        else if (deleteAlias.Contains(GetArgument(data.Arguments, 0)))
                        {
                            commandReturn.SetMessage(await ProcessDeleteEmote(data.Arguments, data.User.Language, data.ChannelId, data.Platform, token, prefix));
                        }
                        else if (renameAlias.Contains(GetArgument(data.Arguments, 0)))
                        {
                            commandReturn.SetMessage(await ProcessRenameEmote(data.Arguments, data.User.Language, data.ChannelId, data.Platform, token, prefix));
                        }
                    }
                    else if (addAlias.Contains(GetArgument(data.Arguments, 0)) || deleteAlias.Contains(GetArgument(data.Arguments, 0)) || renameAlias.Contains(GetArgument(data.Arguments, 0)))
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_rights", data.ChannelId, data.Platform));
                    }
                }
                else
                {
                    if (Program.BotInstance.ChannelsSevenTVEmotes.ContainsKey(data.ChannelId))
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:emotes:7tv:info",
                            data.ChannelId,
                            data.Platform,
                            Program.BotInstance.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count().ToString()
                            ));
                    else
                    {
                        await SevenTvEmoteCache.EmoteUpdate(data.Channel, data.ChannelId);
                        if (Program.BotInstance.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count > 0)
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:emotes:7tv:info",
                                data.ChannelId,
                                data.Platform,
                                Program.BotInstance.ChannelsSevenTVEmotes[data.ChannelId].emotes.Count().ToString()
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
        private string? GetUserID(string id)
        {
            string? from_id = null;

            if (Program.BotInstance.UsersSevenTVIDs is not null && Program.BotInstance.UsersSevenTVIDs.ContainsKey(id))
                from_id = Program.BotInstance.UsersSevenTVIDs[id];
            else
            {
                from_id = Program.BotInstance.SevenTv.SearchUser(UsernameResolver.GetUsername(id, Platform.Twitch, true), Program.BotInstance.Tokens.SevenTV).Result;
                if (from_id != null)
                {
                    if (Program.BotInstance.UsersSevenTVIDs is null)
                        Program.BotInstance.UsersSevenTVIDs = new();

                    Program.BotInstance.UsersSevenTVIDs.Add(id, from_id);
                    Manager.Save(Program.BotInstance.Paths.SevenTVCache, "ids", Program.BotInstance.UsersSevenTVIDs);
                }
            }

            return from_id;
        }
        #endregion

        #region Processes
        private async Task<string> ProcessAddEmote(List<string> arguments, string channelId, Platform platform, Language language, string prefix, string token)
        {
            if (GetArgument(arguments, 1) == null)
            {
                return ShowLowArgsError(language, channelId, $"{prefix}emote add emotename as:alias from:itzkitb", platform);
            }

            var (setId, error) = await GetEmoteSetId(channelId, platform);
            if (setId == null)
            {
                return error;
            }

            string from = GetArgument(arguments, "from");
            string emoteName = GetArgument(arguments, "as") ?? GetArgument(arguments, 1);

            if (FindEmoteInSet(setId, emoteName).Result is not null)
            {
                return LocalizationService.GetString(language, "command:emotes:7tv:add:already", channelId, platform);
            }
            else if (string.IsNullOrWhiteSpace(from))
            {
                return await AddEmoteFromGlobal(setId, emoteName, platform, language, channelId, token);
            }
            else
            {
                return await AddEmoteFromUser(setId, from, emoteName, language, channelId, platform, token);
            }
        }

        private async Task<string> ProcessDeleteEmote(List<string> arguments, Language language, string channelId, Platform platform, string token, string prefix)
        {
            string emoteName = GetArgument(arguments, 1);
            if (string.IsNullOrWhiteSpace(emoteName))
            {
                return ShowLowArgsError(language, channelId, $"{prefix}emote delete emotename", platform);
            }

            var (setId, error) = await GetEmoteSetId(channelId, platform);
            if (setId == null)
            {
                return error;
            }

            var emote = await FindEmoteInSet(setId, emoteName);
            if (emote == null || emote.id == null)
            {
                return LocalizationService.GetString(language, "command:emotes:7tv:not_founded", channelId, platform, emoteName);
            }

            var result = await Program.BotInstance.SevenTv.Remove(setId, emote.id, token);
            if (result && Program.BotInstance.EmotesCache.ContainsKey($"{setId}_{emoteName}"))
            {
                Program.BotInstance.EmotesCache.Remove($"{setId}_{emoteName}", out var cache);
            }

            return ProcessResult(result, "command:emotes:7tv:removed", "command:emotes:7tv:noaccess:editor", emoteName, language, channelId, platform);
        }

        private async Task<string> ProcessRenameEmote(List<string> arguments, Language language, string channelId, Platform platform, string token, string prefix)
        {
            string oldName = GetArgument(arguments, 1);
            string newName = GetArgument(arguments, "as");

            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
            {
                return ShowLowArgsError(language, channelId, $"{prefix}emote rename oldname as:newname", platform);
            }

            var (setId, error) = await GetEmoteSetId(channelId, platform);
            if (setId == null)
            {
                return error;
            }

            var emote = await FindEmoteInSet(setId, oldName);
            if (emote == null || emote.id == null)
            {
                return LocalizationService.GetString(language, "command:emotes:7tv:not_founded", channelId, platform, oldName);
            }

            var result = await Program.BotInstance.SevenTv.Rename(setId, newName, emote.id, token);
            return ProcessResult(result, "command:emotes:7tv:renamed", "command:emotes:7tv:noaccess:editor", $"{oldName} → {newName}", language, channelId, platform);
        }
        #endregion

        #region Help Methods
        private async Task<(string setId, string error)> GetEmoteSetId(string channelId, Platform platform)
        {
            if (Program.BotInstance.EmoteSetsCache.TryGetValue(channelId, out var cached) &&
                DateTime.UtcNow < cached.expiration)
            {
                return (cached.setId, string.Empty);
            }

            try
            {
                var userId = GetUserID(channelId);
                var user = await Program.BotInstance.Clients.SevenTV.rest.GetUser(userId);

                var setId = user.connections[0].emote_set.id;

                Program.BotInstance.EmoteSetsCache[channelId] = (setId, DateTime.UtcNow.Add(Program.BotInstance.CacheTTL));
                return (setId, string.Empty);
            }
            catch (Exception ex)
            {
                Write(ex);
                return (string.Empty, LocalizationService.GetString(Language.EnUs, "command:emotes:7tv:set_error", channelId, platform));
            }
        }

        private async Task<Emote> FindEmoteInSet(string setId, string emoteName)
        {
            var cacheKey = $"{setId}_{emoteName}";
            if (Program.BotInstance.EmotesCache.TryGetValue(cacheKey, out var cached) &&
                DateTime.UtcNow < cached.expiration)
            {
                return cached.emote;
            }

            var set = await Program.BotInstance.Clients.SevenTV.rest.GetEmoteSet(setId);
            var emote = set.emotes.FirstOrDefault(e => e.name.Equals(emoteName));

            if (emote != null)
            {
                Program.BotInstance.EmotesCache[cacheKey] = (emote, DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));
            }

            return emote;
        }
        #endregion

        #region Other Things
        private string ProcessResult(bool result, string successKey, string errorKey, string emoteName, Language language, string channelId, Platform platform)
        {
            return result
                ? LocalizationService.GetString(language, successKey, channelId, platform, emoteName)
                : LocalizationService.GetString(language, errorKey, channelId, platform);
        }

        private string ShowLowArgsError(Language language, string channelId, string commandExample, Platform platform)
        {
            return LocalizationService.GetString(language, "error:not_enough_arguments", channelId, platform)
                .Replace("command_example", commandExample);
        }

        private async Task<string> AddEmoteFromGlobal(string setId, string emoteName, Platform platform, Language language, string channelId, string token)
        {
            var emote = await Program.BotInstance.SevenTv.SearchEmote(emoteName, token);
            if (emote == null)
            {
                return LocalizationService.GetString(language, "command:emotes:7tv:not_founded", channelId, platform, emoteName);
            }

            var result = await Program.BotInstance.SevenTv.Add(setId, emoteName, emote, token);
            return ProcessResult(result, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, language, channelId, platform);
        }

        private async Task<string> AddEmoteFromUser(string setId, string fromUser, string emoteName, Language language, string channelId, Platform platform, string token)
        {
            var sourceUserId = GetUserID(UsernameResolver.GetUserID(fromUser, Platform.Twitch));
            var sourceUser = await Program.BotInstance.Clients.SevenTV.rest.GetUser(sourceUserId);
            var sourceSetId = sourceUser.connections[0].emote_set.id;

            var emote = await FindEmoteInSet(sourceSetId, emoteName);
            if (emote == null)
            {
                return LocalizationService.GetString(language, "command:emotes:7tv:not_founded", channelId, platform, emoteName);
            }

            var result = await Program.BotInstance.SevenTv.Add(setId, emoteName, emote.id, token);
            return ProcessResult(result, "command:emotes:7tv:added", "command:emotes:7tv:noaccess:editor", emoteName, language, channelId, platform);
        }
        #endregion
    }
}
