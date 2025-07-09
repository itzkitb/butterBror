using butterBror.Utils.DataManagers;
using butterBror.Utils.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Tools
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
        public static void Add(string userID, int buttersAdd, int crumbsAdd, Platforms platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            int crumbs = GetSubbalance(userID, platform) + crumbsAdd;
            int butters = GetBalance(userID, platform) + buttersAdd;

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

            UsersData.Save(userID, "floatBalance", crumbs, platform);
            UsersData.Save(userID, "balance", butters, platform);
        }

        /// <summary>
        /// Retrieves the user's main balance (in butters) from persistent storage.
        /// </summary>
        /// <param name="userID">The unique identifier of the user.</param>
        /// <param name="platform">The platform context for the balance retrieval.</param>
        /// <returns>The user's current balance in butters.</returns>
        public static int GetBalance(string userID, Platforms platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return UsersData.Get<int>(userID, "balance", platform);
        }

        /// <summary>
        /// Retrieves the user's fractional balance (in crumbs) from persistent storage.
        /// </summary>
        /// <param name="userID">The unique identifier of the user.</param>
        /// <param name="platform">The platform context for the balance retrieval.</param>
        /// <returns>The user's current fractional balance in crumbs (0-99).</returns>
        public static int GetSubbalance(string userID, Platforms platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return UsersData.Get<int>(userID, "floatBalance", platform);
        }
    }
}
