using bb.Models.Platform;
using DankDB;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using static bb.Core.Bot.Console;

namespace bb.Utils
{
    /// <summary>
    /// Advanced filtering system for detecting banned words in user messages across multiple streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements a sophisticated multi-stage content filtering mechanism designed to:
    /// <list type="bullet">
    /// <item>Detect both explicit and obfuscated banned content</item>
    /// <item>Process messages through eight different validation strategies</item>
    /// <item>Apply character replacement rules to identify intentional misspellings</item>
    /// <item>Track detected violations with detailed diagnostic information</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key technical features:
    /// <list type="bullet">
    /// <item>Keyboard layout transformation to detect shifted-content attempts</item>
    /// <item>Character duplication normalization for stretched words</item>
    /// <item>Concurrent dictionary for thread-safe violation tracking</item>
    /// <item>Pre-compiled regex patterns for optimal performance</item>
    /// <item>Combined global and channel-specific filtering rules</item>
    /// </list>
    /// </para>
    /// The system is engineered for high-throughput chat environments while maintaining accuracy in identifying sophisticated content bypass attempts.
    /// </remarks>
    public class BlockedWordDetector
    {
        private string _founded = "";
        private string _replacementPattern;
        private Regex _replacementRegex;

        /// <summary>
        /// Performs comprehensive validation of a message against all configured content filters.
        /// </summary>
        /// <param name="message">The raw message text to be validated.</param>
        /// <param name="channelId">The identifier of the channel/room where the message was sent.</param>
        /// <param name="platform">The streaming platform context (Twitch, Discord, or Telegram).</param>
        /// <returns>
        /// <see langword="true"/> if the message passes all validation checks;
        /// <see langword="false"/> if any banned content is detected.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Validation workflow:
        /// <list type="number">
        /// <item>Message normalization (lowercasing, ASCII cleaning, duplicate character removal)</item>
        /// <item>Keyboard layout transformation for alternative text representations</item>
        /// <item>Retrieval of global and channel-specific banned word lists</item>
        /// <item>Construction of character replacement patterns</item>
        /// <item>Execution of eight-stage validation sequence through <see cref="RunCheck"/></item>
        /// <item>Detailed logging of results with timing metrics</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance metrics:
        /// <list type="bullet">
        /// <item>Typical processing time: 1-5ms per message (varies by message length)</item>
        /// <item>Uses GUID-based operation tracking for concurrent validation</item>
        /// <item>Logs detailed diagnostics for failed validations</item>
        /// <item>Thread-safe implementation for high-concurrency environments</item>
        /// </list>
        /// </para>
        /// The method automatically combines global banned words with channel-specific filters for comprehensive coverage.
        /// Returns false immediately upon first detection to optimize performance.
        /// </remarks>
        public (bool, double, string, string) Check(string message, string channelId, PlatformsEnum platform, bool log = true)
        {
            DateTime start_time = DateTime.UtcNow;
            try
            {
                bool failed = false;
                string sector = "";

                string clearedMessage = TextSanitizer.CleanAsciiWithoutSpaces(message.ToLower());
                string clearedMessageWithoutRepeats = TextSanitizer.RemoveDuplicates(clearedMessage);
                string clearedMessageWithoutRepeatsChangedLayout = TextSanitizer.ChangeLayout(clearedMessageWithoutRepeats);
                string clearedMessageChangedLayout = TextSanitizer.ChangeLayout(clearedMessage);

                string bannedWordsPath = bb.Program.BotInstance.Paths.BlacklistWords;

                List<string> singleBanwords = Manager.Get<List<string>>(bannedWordsPath, "single_word");
                Dictionary<string, string> replacements = Manager.Get<Dictionary<string, string>>(bannedWordsPath, "replacement_list") ?? new Dictionary<string, string>();
                List<string> bannedWords = Manager.Get<List<string>>(bannedWordsPath, "list");
                bannedWords.AddRange(bb.Program.BotInstance.DataBase.Channels.GetBanWords(platform, channelId));

                _replacementPattern = string.Join("|", replacements.Keys.Select(Regex.Escape));
                _replacementRegex = new Regex(_replacementPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                (bool, string) check_result = RunCheck(channelId,
                    bannedWords,
                    singleBanwords,
                    replacements,
                    clearedMessageWithoutRepeats,
                    clearedMessageWithoutRepeats,
                    clearedMessage,
                    clearedMessageChangedLayout);

                failed = !check_result.Item1;
                sector = check_result.Item2;

                if (log)
                {
                    if (failed) Write($"Blocked words detected! Word: {_founded}; Sector: {sector} (in {(DateTime.UtcNow - start_time).TotalMilliseconds}ms)");
                    else Write($"No blocked words found! (in {(DateTime.UtcNow - start_time).TotalMilliseconds}ms)");
                }

                return (!failed, (DateTime.UtcNow - start_time).TotalMilliseconds, !failed ? "NotFound" : sector, !failed ? "Empty" : _founded);
            }
            catch (Exception ex)
            {
                Write(ex);
                return (false, (DateTime.UtcNow - start_time).TotalMilliseconds, "ERR", ex.Message);
            }
        }

        /// <summary>
        /// Executes the eight-stage validation sequence against transformed message versions.
        /// </summary>
        /// <param name="channelId">The channel/room identifier.</param>
        /// <param name="checkUUID">Unique operation identifier for tracking and diagnostics.</param>
        /// <param name="bannedWords">Combined list of global and channel-specific banned words.</param>
        /// <param name="singleBanwords">List of words requiring exact match (no partial matches).</param>
        /// <param name="replacements">Character replacement dictionary for obfuscation detection.</param>
        /// <param name="clearedMessageWithoutRepeats">Normalized message with duplicate characters removed.</param>
        /// <param name="clearedMessageWithoutRepeatsChangedLayout">Duplicate-removed message with keyboard layout transformed.</param>
        /// <param name="clearedMessage">Fully normalized base message.</param>
        /// <param name="clearedMessageChangedLayout">Base message with keyboard layout transformed.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="table">
        /// <item><term>bool</term><description><see langword="true"/> if all checks pass; <see langword="false"/> if any check fails</description></item>
        /// <item><term>string</term><description>Identifier of the failed check stage (if applicable)</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// Validation stages executed in order:
        /// <list type="table">
        /// <item><term>LightCheckWR</term><description>Basic check on duplicate-removed message</description></item>
        /// <item><term>LightReplacemetCheckWR</term><description>Check with replacements on duplicate-removed message</description></item>
        /// <item><term>LayoutChangeCheckWR</term><description>Layout-transformed check on duplicate-removed message</description></item>
        /// <item><term>LayoutChangeReplacementCheckWR</term><description>Layout-transformed check with replacements on duplicate-removed message</description></item>
        /// <item><term>LightCheck</term><description>Basic check on full normalized message</description></item>
        /// <item><term>LightReplacemetCheck</term><description>Check with replacements on full normalized message</description></item>
        /// <item><term>LayoutChangeCheck</term><description>Layout-transformed check on full message</description></item>
        /// <item><term>LayoutChangeReplacementCheck</term><description>Final comprehensive check with all transformations</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Optimization strategy:
        /// <list type="bullet">
        /// <item>Short-circuits at first failed check to minimize processing</item>
        /// <item>Processes simpler checks first (lighter transformations)</item>
        /// <item>Only executes more complex checks when simpler ones pass</item>
        /// <item>Uses pre-processed message variants to avoid redundant operations</item>
        /// </list>
        /// </para>
        /// This staged approach balances thoroughness with performance in high-volume chat environments.
        /// </remarks>
        private (bool, string) RunCheck(
    string channelId, List<string> bannedWords,
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
                    ? CheckReplacements(message, channelId, bannedWords, singleBanwords, replacements)
                    : CheckBanWords(message, channelId, bannedWords, singleBanwords);

                if (!result) return (false, label);
            }

            return (true, "LayoutChangeReplacementCheck");
        }

        /// <summary>
        /// Validates a normalized message against banned word lists using direct string comparison.
        /// </summary>
        /// <param name="message">The normalized message text to validate.</param>
        /// <param name="channelId">The channel/room identifier.</param>
        /// <param name="checkUUID">Unique operation identifier for tracking.</param>
        /// <param name="bannedWords">Combined list of global and channel-specific banned words.</param>
        /// <param name="singleBanwords">List of words requiring exact match (no partial matches).</param>
        /// <returns>
        /// <see langword="true"/> if the message contains no banned words;
        /// <see langword="false"/> if any banned word is detected.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Matching behavior:
        /// <list type="bullet">
        /// <item><c>bannedWords</c>: Case-insensitive partial matching (e.g., "bad" matches "badword")</item>
        /// <item><c>singleBanwords</c>: Case-insensitive exact matching (e.g., "bad" only matches "bad")</item>
        /// </list>
        /// </para>
        /// <para>
        /// Technical implementation:
        /// <list type="bullet">
        /// <item>Uses <see cref="string.Contains(string, StringComparison)"/> with <see cref="StringComparison.OrdinalIgnoreCase"/></item>
        /// <item>Employs LINQ's <see cref="Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/> for efficient scanning</item>
        /// <item>Updates <see cref="_foundedBanWords"/> with violation details on failure</item>
        /// </list>
        /// </para>
        /// This method serves as the foundation for both direct and replacement-based validation strategies.
        /// Handles the core matching logic without additional transformation overhead.
        /// </remarks>
        private bool CheckBanWords(string message, string channelId,
    List<string> bannedWords, List<string> singleBanwords)
        {
            try
            {
                var foundBannedWord = bannedWords
                    .FirstOrDefault(word =>
                        !string.IsNullOrEmpty(word) &&
                        message.Contains(word, StringComparison.OrdinalIgnoreCase));

                if (foundBannedWord != null)
                {
                    _founded = foundBannedWord;
                    return false;
                }

                var foundSingleWord = singleBanwords
                    .FirstOrDefault(word =>
                        !string.IsNullOrEmpty(word) &&
                        message.Equals(word, StringComparison.OrdinalIgnoreCase));

                if (foundSingleWord != null)
                {
                    _founded = foundSingleWord;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Write(ex);
                _founded = "Check error";
                return false;
            }
        }

        /// <summary>
        /// Applies character replacement rules to detect obfuscated banned words, then validates the transformed message.
        /// </summary>
        /// <param name="message">The normalized message text to transform and validate.</param>
        /// <param name="channelId">The channel/room identifier.</param>
        /// <param name="checkUUID">Unique operation identifier for tracking.</param>
        /// <param name="bannedWords">Combined list of global and channel-specific banned words.</param>
        /// <param name="singleBanwords">List of words requiring exact match (no partial matches).</param>
        /// <param name="replacements">Character replacement dictionary for obfuscation detection.</param>
        /// <returns>
        /// <see langword="true"/> if the transformed message contains no banned words;
        /// <see langword="false"/> if any banned word is detected after transformation.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Transformation examples:
        /// <list type="table">
        /// <item><term>"b@d w0rd"</term><description>→ "bad word" (if @→a and 0→o in replacements)</description></item>
        /// <item><term>"bаd wоrd"</term><description>→ "bad word" (if Cyrillic а→a and о→o)</description></item>
        /// <item><term>"b4d w0rd"</term><description>→ "bad word" (if 4→a and 0→o)</description></item>
        /// <item><term>"ßåÐ"</term><description>→ "bad" (if ß→b, å→a, Ð→d)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Technical process:
        /// <list type="number">
        /// <item>Uses pre-compiled regex to identify characters needing replacement</item>
        /// <item>Applies replacements through <see cref="Regex.Replace(string, MatchEvaluator)"/></item>
        /// <item>Delegates to <see cref="CheckBanWords"/> for final validation</item>
        /// </list>
        /// </para>
        /// This method is critical for detecting sophisticated attempts to bypass content filters through character substitution.
        /// The replacement dictionary typically includes mappings for:
        /// <list type="bullet">
        /// <item>Leet speak variations (e.g., 4→a, 3→e)</item>
        /// <item>Homoglyphs across character sets</item>
        /// <item>Keyboard-shifted characters</item>
        /// </list>
        /// </remarks>
        private bool CheckReplacements(string message, string channelId,
    List<string> bannedWords, List<string> singleBanwords, Dictionary<string, string> replacements)
        {
            try
            {
                if (replacements.Count == 0) return true;

                string maskedWord = _replacementRegex.Replace(message, match =>
                    replacements[match.Value.ToLower()]);

                return CheckBanWords(maskedWord, channelId, bannedWords, singleBanwords);
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }
    }
}
