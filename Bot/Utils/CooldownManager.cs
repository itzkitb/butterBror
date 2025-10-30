using bb.Core.Configuration;
using bb.Models.Platform;
using bb.Models.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static bb.Core.Bot.Logger;

namespace bb.Utils
{
    public class CooldownManager
    {
        /// <summary>
        /// Validates command execution against user-specific and global cooldown constraints.
        /// </summary>
        /// <param name="userCooldown">Per-user cooldown duration in seconds</param>
        /// <param name="globalCooldown">Global cooldown duration in seconds</param>
        /// <param name="cooldownName">Unique identifier for the cooldown parameter</param>
        /// <param name="userID">User identifier to check against</param>
        /// <param name="roomID">Channel/room identifier context</param>
        /// <param name="platform">Target platform (Twitch, Discord, Telegram)</param>
        /// <param name="ignoreUserVIP">If <see langword="true"/>, bypasses VIP/moderator cooldown exemptions</param>
        /// <param name="ignoreGlobalCooldown">If <see langword="true"/>, skips global cooldown verification</param>
        /// <returns>
        /// <see langword="true"/> if command can be executed (cooldown requirements satisfied);
        /// <see langword="false"/> if on cooldown.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Verification sequence:
        /// <list type="number">
        /// <item>Checks VIP/moderator status for potential bypass</item>
        /// <item>Verifies user-specific cooldown timer</item>
        /// <item>Checks global channel cooldown (if not bypassed)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Special behaviors:
        /// <list type="bullet">
        /// <item>First-time users automatically pass cooldown check</item>
        /// <item>Negative cooldown values effectively disable cooldown</item>
        /// <item>Global cooldown only applies if user cooldown passes</item>
        /// <item>Logs detailed cooldown information at warning level</item>
        /// </list>
        /// </para>
        /// <para>
        /// Data storage:
        /// <list type="bullet">
        /// <item>User cooldowns stored in Users.LastUse parameter</item>
        /// <item>Global cooldowns tracked in ChannelsDatabase</item>
        /// <item>Timestamps stored in ISO 8601 format for UTC consistency</item>
        /// </list>
        /// </para>
        /// This method handles all cooldown logic for command execution across all platforms.
        /// </remarks>
        public bool CheckCooldown(
            int userCooldown,
            int globalCooldown,
            string cooldownName,
            string userID,
            string roomID,
            Platform platform,
            bool ignoreUserVIP = false,
            bool ignoreGlobalCooldown = false
        )
        {
            try
            {
                // Dev or mod bypass
                bool isVipOrStaff = (Roles)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userID), Users.Role)) >= Roles.ChatMod;

                if (isVipOrStaff && ignoreUserVIP)
                {
                    return true;
                }

                string lastUsesJson = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userID), Users.LastUse);

                if (lastUsesJson != null)
                {
                    Dictionary<string, string> lastUses = DataConversion.ParseStringDictionary(lastUsesJson);
                    DateTime now = DateTime.UtcNow;

                    // First user use
                    if (!lastUses.ContainsKey(cooldownName))
                    {
                        lastUses.Add(cooldownName, now.ToString("o"));
                        bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userID), Users.LastUse, DataConversion.SerializeStringDictionary(lastUses));
                        return true;
                    }

                    // User cooldown check
                    DateTime lastUserUse = DateTime.Parse(lastUses[cooldownName], null, DateTimeStyles.AdjustToUniversal);
                    double userElapsedSec = (now - lastUserUse).TotalSeconds;
                    if (userElapsedSec < userCooldown)
                    {
                        Write($"#{userID} tried to use the command, but it's on cooldown! (userElapsedSec: {userElapsedSec}, userCooldown: {userCooldown}, now: {now}, lastUserUse: {lastUserUse})", LogLevel.Warning);
                        return false;
                    }

                    // Reset user timer
                    lastUses[cooldownName] = now.ToString("o");
                    bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userID), Users.LastUse, DataConversion.SerializeStringDictionary(lastUses));

                    // Global cooldown bypass
                    if (ignoreGlobalCooldown)
                    {
                        return true;
                    }

                    // Global cooldown check
                    bool isOnGlobalCooldown = !bb.Program.BotInstance.DataBase.Channels.IsCommandCooldown(platform, roomID, cooldownName, globalCooldown);
                    if (!isOnGlobalCooldown)
                    {
                        Write($"#{userID} tried to use the command, but it is on global cooldown!", LogLevel.Warning);
                    }
                    return isOnGlobalCooldown;
                }
                return true;
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }

        /// <summary>
        /// Calculates remaining cooldown time for a specific user command.
        /// </summary>
        /// <param name="userID">User identifier to check</param>
        /// <param name="cooldownName">Cooldown parameter name to query</param>
        /// <param name="userSecondsCooldown">Total cooldown duration in seconds</param>
        /// <param name="platform">Target platform context</param>
        /// <returns>
        /// <see cref="TimeSpan"/> representing remaining cooldown duration.
        /// Returns <see cref="TimeSpan.Zero"/> if no active cooldown or error occurs.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Calculation logic:
        /// <list type="bullet">
        /// <item>Retrieves last execution timestamp from user data</item>
        /// <item>Computes elapsed time since last execution</item>
        /// <item>Subtracts elapsed time from total cooldown duration</item>
        /// </list>
        /// </para>
        /// <para>
        /// Edge cases:
        /// <list type="bullet">
        /// <item>Returns zero if user has never used the command</item>
        /// <item>Returns zero for negative or zero cooldown durations</item>
        /// <item>Returns zero on data access errors (safe default)</item>
        /// <item>Negative results indicate cooldown has expired</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>Displaying remaining cooldown in command responses</item>
        /// <item>Cooldown visualization in UI elements</item>
        /// <item>Conditional command availability checks</item>
        /// </list>
        /// </para>
        /// Time calculations use UTC for consistent timezone handling across distributed systems.
        /// </remarks>
        public TimeSpan GetCooldownTime(
            string userID,
            string cooldownName,
            int userSecondsCooldown,
            Platform platform
        )
        {
            try
            {
                Dictionary<string, string> LastUses = DataConversion.ParseStringDictionary((string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userID), Users.LastUse));
                if (LastUses.TryGetValue(cooldownName, out var lastUse))
                {
                    return TimeSpan.FromSeconds(userSecondsCooldown) - (DateTime.UtcNow - DateTime.Parse(lastUse, null, DateTimeStyles.AdjustToUniversal));
                }
                else
                {
                    return TimeSpan.FromSeconds(0);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                return new TimeSpan(0);
            }
        }
    }
}
