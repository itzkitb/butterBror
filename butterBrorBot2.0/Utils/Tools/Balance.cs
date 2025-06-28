using butterBror.Utils.DataManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Tools
{
    public class Balance
    {
        /// <summary>
        /// Add/reduce user balance
        /// </summary>
        public static void Add(string userID, int buttersAdd, int crumbsAdd, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            int crumbs = GetSubbalance(userID, platform) + crumbsAdd;
            int butters = GetBalance(userID, platform) + buttersAdd;

            Core.Coins += buttersAdd + crumbsAdd / 100f;
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
        /// Getting user balance
        /// </summary>
        public static int GetBalance(string userID, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            return UsersData.Get<int>(userID, "balance", platform);
        }
        /// <summary>
        /// Getting user balance float
        /// </summary>
        public static int GetSubbalance(string userID, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            return UsersData.Get<int>(userID, "floatBalance", platform);
        }
    }
}
