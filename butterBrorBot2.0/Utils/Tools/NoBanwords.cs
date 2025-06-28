using butterBror.Utils.DataManagers;
using butterBror.Utils;
using DankDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;
using System.Text.RegularExpressions;

namespace butterBror.Utils.Tools
{
    public class NoBanwords
    {
        private readonly ConcurrentDictionary<string, string> FoundedBanWords = new();
        private string replacementPattern;
        private Regex replacementRegex;

        [ConsoleSector("butterBror.Utils.Tools.NoBanwords", "Check")]
        public bool Check(string message, string channelID, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
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

                string banned_words_path = Core.Bot.Pathes.BlacklistWords;
                string channel_banned_words_path = Core.Bot.Pathes.Channels + Platform.strings[(int)platform] + "/" + channelID + "/BANWORDS.json";
                string replacement_path = Core.Bot.Pathes.BlacklistReplacements;

                List<string> single_banwords = Manager.Get<List<string>>(banned_words_path, "single_word");
                Dictionary<string, string> replacements = Manager.Get<Dictionary<string, string>>(replacement_path, "list") ?? new Dictionary<string, string>();
                List<string> banned_words = Manager.Get<List<string>>(banned_words_path, "list");
                if (FileUtil.FileExists(channel_banned_words_path))
                    banned_words.AddRange(Manager.Get<List<string>>(channel_banned_words_path, "list"));

                replacementPattern = string.Join("|", replacements.Keys.Select(Regex.Escape));
                replacementRegex = new Regex(replacementPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

                if (failed) Write($"NoBanwords - [{check_UUID}] BANWORDS WAS FOUNDED! Banword: {FoundedBanWords[check_UUID]}, sector: {sector} ({(DateTime.UtcNow - start_time).TotalMilliseconds}ms)", "info");
                else Write($"NoBanwords - [{check_UUID}] Succeful! ({(DateTime.UtcNow - start_time).TotalMilliseconds}ms)", "info");

                return !failed;
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.NoBanwords", "RunCheck")]
        private (bool, string) RunCheck(
    string channelID, string check_UUID, List<string> banned_words,
    List<string> single_banwords, Dictionary<string, string> replacements,
    string cleared_message_without_repeats, string cleared_message_without_repeats_changed_layout,
    string cleared_message, string cleared_message_changed_layout)
        {
            var checks = new[]
            {
        (message: cleared_message_without_repeats, useReplacement: false, label: "LightCheckWR"),
        (cleared_message_without_repeats, true, "LightReplacemetCheckWR"),
        (cleared_message_without_repeats_changed_layout, false, "LayoutChangeCheckWR"),
        (cleared_message_without_repeats_changed_layout, true, "LayoutChangeReplacementCheckWR"),
        (cleared_message, false, "LightCheck"),
        (cleared_message, true, "LightReplacemetCheck"),
        (cleared_message_changed_layout, false, "LayoutChangeCheck"),
        (cleared_message_changed_layout, true, "LayoutChangeReplacementCheck")
    };

            foreach (var (message, useReplacement, label) in checks)
            {
                bool result = useReplacement
                    ? CheckReplacements(message, channelID, check_UUID, banned_words, single_banwords, replacements)
                    : CheckBanWords(message, channelID, check_UUID, banned_words, single_banwords);

                if (!result) return (false, label);
            }

            return (true, "LayoutChangeReplacementCheck");
        }

        [ConsoleSector("butterBror.Utils.Tools.NoBanwords", "CheckBanWords")]
        private bool CheckBanWords(string message, string channelID, string check_UUID,
    List<string> banned_words, List<string> single_banwords)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (banned_words.Any(word => message.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    FoundedBanWords.TryAdd(check_UUID, "BannedWordFound");
                    return false;
                }

                if (single_banwords.Any(word => message.Equals(word, StringComparison.OrdinalIgnoreCase)))
                {
                    FoundedBanWords.TryAdd(check_UUID, "SingleBannedWordFound");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Write(ex);
                FoundedBanWords.TryAdd(check_UUID, "CHECK ERROR");
                return false;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.NoBanwords", "CheckReplacements")]
        private bool CheckReplacements(string message, string ChannelID, string check_UUID,
    List<string> banned_words, List<string> single_banwords, Dictionary<string, string> replacements)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string maskedWord = replacementRegex.Replace(message, match =>
                    replacements[match.Value.ToLower()]);

                return CheckBanWords(maskedWord, ChannelID, check_UUID, banned_words, single_banwords);
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }
    }
}
