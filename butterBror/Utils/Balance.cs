using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Core.Commands.List;
using butterBror.Data;
using butterBror.Models;
using Microsoft.CodeAnalysis;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides functionality for managing user balances with butters and crumbs currency system.
    /// </summary>
    public class Balance
    {
        /// <summary>
        /// Adds or reduces user balance with automatic conversion between butters and crumbs.
        /// </summary>
        /// <param name="userID">The unique identifier of the user.</param>
        /// <param name="buttersAdd">Amount of butters to add (can be negative for reduction).</param>
        /// <param name="crumbsAdd">Amount of crumbs to add (can be negative for reduction).</param>
        /// <param name="platform">The platform context for the balance operation.</param>
        /// <remarks>
        /// Converts between butters and crumbs when thresholds exceed 100:
        /// - 100 crumbs = 1 butter
        /// - Handles underflow/overflow with negative balance adjustments
        /// Updates both main balance and float balance in user data storage
        /// </remarks>
        public static void Add(string userID, long buttersAdd, long crumbsAdd, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            long crumbs = GetSubbalance(userID, platform) + crumbsAdd;
            long butters = GetBalance(userID, platform) + buttersAdd;

            Engine.Coins += buttersAdd + crumbsAdd / 100f;
            while (crumbs > 100)
            {
                crumbs -= 100;
                butters += 1;
            }

            while (crumbs < 0)
            {
                crumbs += 100;
                butters -= 1;
            }

            Engine.Bot.SQL.Users.SetParameter(platform, Format.ToLong(userID), Users.AfterDotBalance, crumbs);
            Engine.Bot.SQL.Users.SetParameter(platform, Format.ToLong(userID), Users.Balance, butters);
        }

        /// <summary>
        /// Retrieves the user's main balance (in butters) from persistent storage.
        /// </summary>
        /// <param name="userID">The unique identifier of the user.</param>
        /// <param name="platform">The platform context for the balance retrieval.</param>
        /// <returns>The user's current balance in butters.</returns>
        public static long GetBalance(string userID, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return (long)Engine.Bot.SQL.Users.GetParameter(platform, Format.ToLong(userID), Users.Balance);
        }

        /// <summary>
        /// Retrieves the user's fractional balance (in crumbs) from persistent storage.
        /// </summary>
        /// <param name="userID">The unique identifier of the user.</param>
        /// <param name="platform">The platform context for the balance retrieval.</param>
        /// <returns>The user's current fractional balance in crumbs (0-99).</returns>
        public static long GetSubbalance(string userID, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return (long)Engine.Bot.SQL.Users.GetParameter(platform, Format.ToLong(userID), Users.AfterDotBalance);
        }
    }
}
