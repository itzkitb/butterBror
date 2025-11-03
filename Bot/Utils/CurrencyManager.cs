using bb.Core.Configuration;
using bb.Models.Currency;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Services.External;
using Newtonsoft.Json;
using static bb.Core.Bot.Logger;

namespace bb.Utils
{
    /// <summary>
    /// Provides currency management functionality for the dual-unit butters and crumbs economic system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements a two-tier currency system where:
    /// <list type="bullet">
    /// <item><b>Butters</b>: Main currency unit representing whole values</item>
    /// <item><b>Crumbs</b>: Fractional unit where 100 crumbs = 1 butter</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item>Automatic unit conversion during balance operations</item>
    /// <item>Platform-specific balance tracking (Twitch, Discord, Telegram)</item>
    /// <item>Atomic balance updates through buffer system</item>
    /// <item>Global currency metrics tracking (Bot.Coins)</item>
    /// </list>
    /// </para>
    /// The implementation ensures mathematical consistency during all operations while maintaining 
    /// compatibility with the system's floating-point based global currency metrics.
    /// </remarks>
    public class CurrencyManager
    {
        /// <summary>
        /// Adjusts a user's balance by specified amounts of butters and crumbs with automatic unit conversion.
        /// </summary>
        /// <param name="userID">The unique user identifier across all platforms</param>
        /// <param name="buttersAdd">Net change in butters (positive to add, negative to deduct)</param>
        /// <param name="crumbsAdd">Net change in crumbs (positive to add, negative to deduct)</param>
        /// <param name="platform">The platform context for the balance operation</param>
        /// <remarks>
        /// <para>
        /// Conversion process:
        /// <list type="number">
        /// <item>Initial balance values are retrieved from persistent storage</item>
        /// <item>Requested amounts are added to current balances</item>
        /// <item>Automatic normalization occurs when crumbs exceed thresholds:</item>
        /// <item>Updated values are persisted to storage buffers</item>
        /// <item>Global currency metrics are updated with precise fractional values</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Handles negative balances through underflow conversion (e.g., -50 crumbs becomes +50 crumbs with -1 butter)</item>
        /// <item>Maintains crumbs value strictly within 0-99 range after operations</item>
        /// <item>Updates global <see cref="Bot.Coins"/> with fractional precision (1 butter = 1.0, 1 crumb = 0.01)</item>
        /// <item>Operations are buffered for batch persistence to improve database performance</item>
        /// </list>
        /// </para>
        /// This method is designed to handle all valid arithmetic operations while maintaining 
        /// currency system integrity. It's the primary interface for all balance modifications.
        /// </remarks>
        /// <example>
        /// To add 1 butter and 150 crumbs to a user's balance:
        /// <code>
        /// Balance.Add("12345", 1, 150, PlatformsEnum.Twitch);
        /// // Results in: 2 butters, 50 crumbs (150 crumbs = 1 butter + 50 crumbs)
        /// </code>
        /// </example>
        /// <example>
        /// To deduct 2 butters and 30 crumbs from a user's balance:
        /// <code>
        /// Balance.Add("12345", -2, -30, PlatformsEnum.Discord);
        /// // If original balance was 3 butters, 20 crumbs:
        /// // Results in: 0 butters, 90 crumbs (borrows 1 butter to cover crumb deficit)
        /// </code>
        /// </example>
        public void Add(string userID, decimal add, Platform platform)
        {
            decimal balance = Get(userID, platform) + add;

            bb.Program.BotInstance.Coins += add;
            bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userID), Users.Balance, balance);
        }

        /// <summary>
        /// Retrieves a user's main currency balance in whole butters.
        /// </summary>
        /// <param name="userID">The unique user identifier across all platforms</param>
        /// <param name="platform">The platform context for the balance retrieval</param>
        /// <returns>The user's current balance expressed in whole butters (integer value)</returns>
        /// <remarks>
        /// <para>
        /// Important characteristics:
        /// <list type="bullet">
        /// <item>Returns only the whole-number portion of the balance (ignores crumbs)</item>
        /// <item>May return negative values if user has deficit balance</item>
        /// <item>Retrieves data from in-memory buffers for performance</item>
        /// <item>Values are eventually persisted to database storage</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage considerations:
        /// <list type="bullet">
        /// <item>For complete balance information, also call <see cref="GetSubbalance(string, Platform)"/></item>
        /// <item>Not suitable for precise financial calculations requiring fractional values</item>
        /// <item>Typically used for display purposes where whole numbers are preferred</item>
        /// </list>
        /// </para>
        /// This method provides efficient access to the primary balance component with minimal overhead.
        /// </remarks>
        /// <example>
        /// For a balance of 5 butters and 75 crumbs:
        /// <code>
        /// long butters = Balance.GetBalance("12345", PlatformsEnum.Twitch); // Returns 5
        /// </code>
        /// </example>
        public decimal Get(string userID, Platform platform)
        {
            return DataConversion.ToDecimal(bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userID), Users.Balance).ToString());
        }

        public async Task GenerateRandomEventAsync()
        {
            try
            {
                if (bb.Program.BotInstance.TwitchCurrencyRandomEvent == null || bb.Program.BotInstance.TwitchCurrencyRandomEvent.Count == 0)
                {
                    Write("No channels configured for random events", LogLevel.Warning);
                    return;
                }

                const string aiRequest = @"
            Generate a JSON object with exactly two keys: ""text"" and ""added"".
            The ""text"" field must contain 1-2 sentences in English describing a unique, unexpected event related to BTR (Butter).
            Avoid generic terms like ""integration"", ""user growth"", or financial metrics.
            Focus on niche scenarios such as:
            - Virtual art auctions using BTR
            - Community-led environmental initiatives
            - Quirky meme-driven transactions
            Ensure no numerical values or digits appear in the text.
            The ""added"" field must be a random integer between 100 and 1000.
            Only output the JSON object with no additional text or formatting.";

                var aiResponse = await bb.Program.BotInstance.AiService.Request(
                    request: aiRequest,
                    model: "qwen",
                    platform: Platform.Twitch,
                    repetitionPenalty: 1.0,
                    includeChatHistory: false,
                    includeSystemPrompt: false,
                    username: "♿",
                    userId: "♿",
                    language: Language.EnUs
                );

                if (aiResponse[0] == "ERR")
                {
                    Write($"AI service error: {aiResponse[1]}", LogLevel.Error);
                    await SendFallbackRandomEventMessage();
                    return;
                }

                RandomEvent? randomEvent;
                try
                {
                    randomEvent = JsonConvert.DeserializeObject<RandomEvent>(aiResponse[1]);
                }
                catch (JsonException ex)
                {
                    Write($"Failed to deserialize AI response: {ex.Message}", LogLevel.Error);
                    await SendFallbackRandomEventMessage();
                    return;
                }

                if (randomEvent == null || string.IsNullOrEmpty(randomEvent.Text) ||
                    randomEvent.Added < 100 || randomEvent.Added > 1000)
                {
                    Write($"Invalid AI response: {aiResponse[1]}", LogLevel.Warning);
                    await SendFallbackRandomEventMessage();
                    return;
                }

                await SendRandomEventMessagesAsync(randomEvent);
                bb.Program.BotInstance.InBankDollars += randomEvent.Added;
            }
            catch (Exception ex)
            {
                Write($"Critical error in GenerateRandomEventAsync: {ex}", LogLevel.Critical);
                await SendFallbackRandomEventMessage();
            }
        }

        private async Task SendRandomEventMessagesAsync(RandomEvent randomEvent)
        {
            var tasks = new List<Task>();

            foreach (string channelId in bb.Program.BotInstance.TwitchCurrencyRandomEvent)
            {
                try
                {
                    string channelName = UsernameResolver.GetUsername(channelId, Platform.Twitch, true);
                    Language channelLang = (Language?)bb.Program.BotInstance.DataBase.Users.GetParameter(Platform.Twitch, DataConversion.ToLong(channelId), Users.Language) ?? Language.EnUs;

                    string message = LocalizationService.GetString(
                        channelLang,
                        "currency:random_event",
                        channelId,
                        Platform.Twitch,
                        randomEvent.Text,
                        randomEvent.Added,
                        channelName
                    );

                    bb.Program.BotInstance.MessageSender.Send(Platform.Twitch, message, channelName, channelId, channelLang, isSafe: true, isReply: false, addUsername: false);
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Write($"Error sending random event to channel {channelId}: {ex}", LogLevel.Error);
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task SendFallbackRandomEventMessage()
        {
            var tasks = new List<Task>();

            foreach (string channelId in bb.Program.BotInstance.TwitchCurrencyRandomEvent)
            {
                try
                {
                    string channelName = UsernameResolver.GetUsername(channelId, Platform.Twitch, true);
                    Language channelLang = (Language?)bb.Program.BotInstance.DataBase.Users.GetParameter(Platform.Twitch, DataConversion.ToLong(channelId), Users.Language) ?? Language.EnUs;


                    string message = LocalizationService.GetString(
                        channelLang,
                        "currency:random_event",
                        channelId,
                        Platform.Twitch,
                        "Nothing happened today.",
                        0,
                        channelName
                    );

                    bb.Program.BotInstance.MessageSender.Send(Platform.Twitch, message, channelName, channelId, channelLang, isSafe: true, isReply: false, addUsername: false);
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Write($"Error sending fallback message to channel {channelId}: {ex}", LogLevel.Error);
                }
            }

            await Task.WhenAll(tasks);
        }

        public async Task CollectTaxesAsync()
        {
            try
            {
                if (bb.Program.BotInstance.TaxesCost <= 0)
                {
                    Write($"Invalid tax configuration: TaxesCost={bb.Program.BotInstance.TaxesCost}", LogLevel.Warning);
                    return;
                }

                if (bb.Program.BotInstance.Users <= 0)
                {
                    Write($"Invalid user count for tax calculation: {bb.Program.BotInstance.Users}", LogLevel.Warning);
                    return;
                }

                int collectedTaxes = (int)Math.Ceiling(bb.Program.BotInstance.TaxesCost * bb.Program.BotInstance.Users);

                if (bb.Program.BotInstance.TwitchTaxesEvent == null || bb.Program.BotInstance.TwitchTaxesEvent.Count == 0)
                {
                    Write("No channels configured for tax collection", LogLevel.Warning);
                    return;
                }

                await SendTaxMessagesAsync(collectedTaxes);
                bb.Program.BotInstance.InBankDollars += collectedTaxes;
            }
            catch (Exception ex)
            {
                Write($"Critical error in CollectTaxesAsync: {ex}", LogLevel.Critical);
            }
        }

        private async Task SendTaxMessagesAsync(int collectedTaxes)
        {
            var tasks = new List<Task>();

            foreach (string channelId in bb.Program.BotInstance.TwitchTaxesEvent)
            {
                try
                {
                    string channelName = UsernameResolver.GetUsername(channelId, Platform.Twitch, true);
                    Language channelLang = (Language?)bb.Program.BotInstance.DataBase.Users.GetParameter(Platform.Twitch, DataConversion.ToLong(channelId), Users.Language) ?? Language.EnUs;

                    string message = LocalizationService.GetString(
                        channelLang,
                        "currency:taxes",
                        channelId,
                        Platform.Twitch,
                        collectedTaxes,
                        channelName
                    );

                    bb.Program.BotInstance.MessageSender.Send(Platform.Twitch, message, channelName, channelId, channelLang, isSafe: true, isReply: false, addUsername: false);
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Write($"Error sending tax message to channel {channelId}: {ex}", LogLevel.Error);
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}
