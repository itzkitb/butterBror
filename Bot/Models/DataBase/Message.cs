
namespace bb.Models.DataBase
{
    /// <summary>
    /// Represents chat message data.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the date/time when the message was sent.
        /// </summary>
        public DateTime messageDate { get; set; }

        /// <summary>
        /// Gets or sets the text content of the message.
        /// </summary>
        public string messageText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message was sent by the bot itself.
        /// </summary>
        public bool isMe { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sender is a moderator.
        /// </summary>
        public bool isModerator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sender is a subscriber.
        /// </summary>
        public bool isSubscriber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sender is a partner.
        /// </summary>
        public bool isPartner { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sender is Twitch staff.
        /// </summary>
        public bool isStaff { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sender has Twitch Turbo status.
        /// </summary>
        public bool isTurbo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sender is a VIP in the channel.
        /// </summary>
        public bool isVip { get; set; }
    }
}
