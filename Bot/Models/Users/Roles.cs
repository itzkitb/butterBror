using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bb.Models.Users
{
    public enum Roles
    {
        /// <summary>
        /// The user has been blocked and cannot use the bot.
        /// </summary>
        Blocked,

        /// <summary>
        /// The user is a bot and cannot execute commands.
        /// </summary>
        Bot,

        /// <summary>
        /// Assigned to everyone from Twitch by default. Command cooldowns have been doubled.
        /// </summary>
        Public,

        /// <summary>
        /// The moderator of the chat from which the command was executed.
        /// </summary>
        ChatMod,

        /// <summary>
        /// Bot moderator.
        /// </summary>
        BotMod,

        /// <summary>
        /// Bot owner.
        /// </summary>
        BotOwner
    }
}
