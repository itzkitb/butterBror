using DankDB;
using System.Collections.Concurrent;
using static butterBror.Core.Bot.Console;
using System.Text.RegularExpressions;
using butterBror.Data;
using butterBror.Models;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides functionality for checking messages against banned word lists with pattern replacement capabilities.
    /// </summary>
    public class NoBanwords
    {
        private readonly ConcurrentDictionary<string, string> _foundedBanWords = new();
        private string _replacementPattern;
        private Regex _replacementRegex;

        /// <summary>
        /// Checks if a message contains any banned words after applying text transformations.
        /// </summary>
        /// <param name="message">The message text to check.</param>
        /// <param name="channelID">The channel/room identifier.</param>
        /// <param name="platform">Target platform context (Twitch/Discord/Telegram).</param>
        /// <returns>True if message passes checks, false if banned word found.</returns>
        /// <remarks>
        /// - Performs multiple checks with different text transformations
        /// - Uses both global and channel-specific banned word lists
        /// - Applies character replacements before final check
        /// - Logs detailed information about detected banned words
        /// </remarks>
        
        public bool Check(string message, string channelID, PlatformsEnum platform)
        {
            try
            {
                bool failed = false;
                string sector = "";
                DateTime start_time = DateTime.UtcNow;

                string check_UUID = Guid.NewGuid().ToString();

                string cleared_message = Text.CleanAsciiWithoutSpaces(message.ToLower());
                string cleared_message_without_repeats = Text.RemoveDuplicates(cleared_message);
                string cleared_message_without_repeats_changed_layout = Text.ChangeLayout(cleared_message_without_repeats);
                string cleared_message_changed_layout = Text.ChangeLayout(cleared_message);

                string banned_words_path = Engine.Bot.Pathes.BlacklistWords;
                string replacement_path = Engine.Bot.Pathes.BlacklistReplacements;

                List<string> single_banwords = Manager.Get<List<string>>(banned_words_path, "single_word");
                Dictionary<string, string> replacements = Manager.Get<Dictionary<string, string>>(replacement_path, "list") ?? new Dictionary<string, string>();
                List<string> banned_words = Manager.Get<List<string>>(banned_words_path, "list");
                banned_words.AddRange(Engine.Bot.SQL.Channels.GetBanWords(platform, channelID));

                _replacementPattern = string.Join("|", replacements.Keys.Select(Regex.Escape));
                _replacementRegex = new Regex(_replacementPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                (bool, string) check_result = RunCheck(channelID,
                    check_UUID,
                    banned_words,
                    single_banwords,
                    replacements,
                    cleared_message_without_repeats,
                    cleared_message_without_repeats,
                    cleared_message,
                    cleared_message_changed_layout);

                failed = !check_result.Item1;
                sector = check_result.Item2;

                if (failed) Write($"NoBanwords - [{check_UUID}] BANWORDS WAS FOUNDED! Banword: {_foundedBanWords[check_UUID]}, sector: {sector} ({(DateTime.UtcNow - start_time).TotalMilliseconds}ms)", "info");
                else Write($"NoBanwords - [{check_UUID}] Succeful! ({(DateTime.UtcNow - start_time).TotalMilliseconds}ms)", "info");

                return !failed;
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }

        /// <summary>
        /// Runs multiple banword checks using different text processing strategies.
        /// </summary>
        /// <param name="channelID">The channel/room identifier.</param>
        /// <param name="checkUUID">Unique identifier for tracking this check operation.</param>
        /// <param name="bannedWords">List of global banned words.</param>
        /// <param name="singleBanwords">List of single-word bans.</param>
        /// <param name="replacements">Dictionary of character replacements to apply.</param>
        /// <param name="clearedMessageWithoutRepeats">Message with duplicates removed.</param>
        /// <param name="clearedMessageWithoutRepeatsChangedLayout">Message with duplicates removed and layout changed.</param>
        /// <param name="clearedMessage">Original cleaned message.</param>
        /// <param name="clearedMessageChangedLayout">Message with layout changed.</param>
        /// <returns>Tuple containing check result and failed check type.</returns>
        /// <remarks>
        /// Performs 8 different checks in sequence:
        /// 1. Light check without replacements
        /// 2. Light check with replacements
        /// 3. Layout-changed check without replacements
        /// 4. Layout-changed check with replacements
        /// 5. Full message check without replacements
        /// 6. Full message check with replacements
        /// 7. Layout-changed full message check
        /// 8. Final layout-changed check with replacements
        /// </remarks>
        
        private (bool, string) RunCheck(
    string channelID, string checkUUID, List<string> bannedWords,
    List<string> singleBanwords, Dictionary<string, string> replacements,
    string clearedMessageWithoutRepeats, string clearedMessageWithoutRepeatsChangedLayout,
    string clearedMessage, string clearedMessageChangedLayout)
        {
            var checks = new[]
            {
        (message: clearedMessageWithoutRepeats, useReplacement: false, label: "LightCheckWR"),
        (clearedMessageWithoutRepeats, true, "LightReplacemetCheckWR"),
        (clearedMessageWithoutRepeatsChangedLayout, false, "LayoutChangeCheckWR"),
        (clearedMessageWithoutRepeatsChangedLayout, true, "LayoutChangeReplacementCheckWR"),
        (clearedMessage, false, "LightCheck"),
        (clearedMessage, true, "LightReplacemetCheck"),
        (clearedMessageChangedLayout, false, "LayoutChangeCheck"),
        (clearedMessageChangedLayout, true, "LayoutChangeReplacementCheck")
    };

            foreach (var (message, useReplacement, label) in checks)
            {
                bool result = useReplacement
                    ? CheckReplacements(message, channelID, checkUUID, bannedWords, singleBanwords, replacements)
                    : CheckBanWords(message, channelID, checkUUID, bannedWords, singleBanwords);

                if (!result) return (false, label);
            }

            return (true, "LayoutChangeReplacementCheck");
        }

        /// <summary>
        /// Checks message against banned words without character replacement.
        /// </summary>
        /// <param name="message">The message text to check.</param>
        /// <param name="channelID">The channel/room identifier.</param>
        /// <param name="checkUUID">Unique identifier for tracking this check operation.</param>
        /// <param name="bannedWords">List of global banned words.</param>
        /// <param name="singleBanwords">List of single-word bans.</param>
        /// <returns>True if message is clean, false if banned word found.</returns>
        /// <remarks>
        /// - Checks for both partial matches and exact matches
        /// - Uses case-insensitive comparison
        /// - Updates internal cache with detected banned words
        /// </remarks>
        
        private bool CheckBanWords(string message, string channelID, string checkUUID,
    List<string> bannedWords, List<string> singleBanwords)
        {
            try
            {
                if (bannedWords.Any(word => message.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    _foundedBanWords.TryAdd(checkUUID, "BannedWordFound");
                    return false;
                }

                if (singleBanwords.Any(word => message.Equals(word, StringComparison.OrdinalIgnoreCase)))
                {
                    _foundedBanWords.TryAdd(checkUUID, "SingleBannedWordFound");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Write(ex);
                _foundedBanWords.TryAdd(checkUUID, "CHECK ERROR");
                return false;
            }
        }

        /// <summary>
        /// Applies character replacements and then checks for banned words.
        /// </summary>
        /// <param name="message">The message text to check.</param>
        /// <param name="channelID">The channel/room identifier.</param>
        /// <param name="checkUUID">Unique identifier for tracking this check operation.</param>
        /// <param name="bannedWords">List of global banned words.</param>
        /// <param name="singleBanwords">List of single-word bans.</param>
        /// <param name="replacements">Character replacement dictionary.</param>
        /// <returns>True if message is clean after replacements, false if banned word found.</returns>
        /// <remarks>
        /// - Applies character replacements using regex pattern
        /// - Delegates to CheckBanWords for final validation
        /// - Uses compiled regex for better performance
        /// </remarks>
        
        private bool CheckReplacements(string message, string channelID, string checkUUID,
    List<string> bannedWords, List<string> singleBanwords, Dictionary<string, string> replacements)
        {
            try
            {
                string maskedWord = _replacementRegex.Replace(message, match =>
                    replacements[match.Value.ToLower()]);

                return CheckBanWords(maskedWord, channelID, checkUUID, bannedWords, singleBanwords);
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }
    }
}
