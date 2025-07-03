using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Enums;

namespace butterBror
{
    /// <summary>
    /// Represents the result of a command execution with customizable output options.
    /// </summary>
    public class CommandReturn
    {
        /// <summary>
        /// Converts a ChatColorPresets value to a corresponding System.Drawing.Color.
        /// </summary>
        /// <param name="color">The preset color to convert.</param>
        /// <returns>The System.Drawing.Color value for the specified preset.</returns>
        /// <remarks>
        /// Returns predefined Twitch-compatible colors with full opacity (ARGB: 255).
        /// Returns black as default for unknown color presets.
        /// </remarks>
        private System.Drawing.Color TwitchColors(ChatColorPresets color)
        {
            switch (color)
            {
                case ChatColorPresets.Blue:
                    return System.Drawing.Color.FromArgb(255, 0, 52, 252);
                case ChatColorPresets.BlueViolet:
                    return System.Drawing.Color.FromArgb(255, 146, 0, 255);
                case ChatColorPresets.CadetBlue:
                    return System.Drawing.Color.FromArgb(255, 85, 140, 135);
                case ChatColorPresets.Chocolate:
                    return System.Drawing.Color.FromArgb(255, 255, 127, 36);
                case ChatColorPresets.Coral:
                    return System.Drawing.Color.FromArgb(255, 255, 127, 80);
                case ChatColorPresets.DodgerBlue:
                    return System.Drawing.Color.FromArgb(255, 30, 144, 255);
                case ChatColorPresets.Firebrick:
                    return System.Drawing.Color.FromArgb(255, 178, 34, 34);
                case ChatColorPresets.GoldenRod:
                    return System.Drawing.Color.FromArgb(255, 218, 165, 32);
                case ChatColorPresets.Green:
                    return System.Drawing.Color.FromArgb(255, 0, 128, 0);
                case ChatColorPresets.HotPink:
                    return System.Drawing.Color.FromArgb(255, 255, 105, 180);
                case ChatColorPresets.OrangeRed:
                    return System.Drawing.Color.FromArgb(255, 255, 69, 0);
                case ChatColorPresets.Red:
                    return System.Drawing.Color.FromArgb(255, 255, 0, 0);
                case ChatColorPresets.SeaGreen:
                    return System.Drawing.Color.FromArgb(255, 46, 139, 87);
                case ChatColorPresets.SpringGreen:
                    return System.Drawing.Color.FromArgb(255, 0, 255, 127);
                case ChatColorPresets.YellowGreen:
                    return System.Drawing.Color.FromArgb(255, 154, 205, 50);
                default:
                    return System.Drawing.Color.FromArgb(255, 0, 0, 0);
            }
        }

        /// <summary>
        /// Initializes a new instance of the CommandReturn class with default values.
        /// </summary>
        /// <remarks>
        /// Sets default values:
        /// - Message: "PauseChamp Empty result..."
        /// - Color: YellowGreen preset and green embed
        /// - Non-embed, non-ephemeral mode
        /// </remarks>
        public CommandReturn()
        {
            this.Message = "PauseChamp Empty result. Report that to @ItzKITb";
            this.IsSafe = false;
            this.Description = string.Empty;
            this.Author = string.Empty;
            this.ImageLink = string.Empty;
            this.ThumbnailLink = string.Empty;
            this.Footer = string.Empty;
            this.IsEmbed = false;
            this.IsEphemeral = false;
            this.Title = string.Empty;
            this.EmbedColor = System.Drawing.Color.Green;
            this.BotNameColor = ChatColorPresets.YellowGreen;
            this.IsError = false;
            this.Exception = null;
        }

        /// <summary>
        /// Sets the message text for the command response.
        /// </summary>
        /// <param name="Message">The message content to display.</param>
        public void SetMessage(string Message)
        {
            this.Message = Message;
        }

        /// <summary>
        /// Sets the message text and safety flag for command response.
        /// </summary>
        /// <param name="Message">The message content to display.</param>
        /// <param name="IsSafe">Indicates whether to bypass banword checks.</param>
        public void SetMessage(string Message, bool IsSafe)
        {
            this.Message = Message;
            this.IsSafe = IsSafe;
        }

        /// <summary>
        /// Sets message text, title, and safety flag for command response.
        /// </summary>
        /// <param name="Message">The message content to display.</param>
        /// <param name="Title">The title for embedded messages.</param>
        /// <param name="IsSafe">Indicates whether to bypass banword checks.</param>
        public void SetMessage(string Message, string Title, bool IsSafe)
        {
            this.Message = Message;
            this.IsSafe = IsSafe;
            this.Title = Title;
        }

        /// <summary>
        /// Marks the response as an error with associated exception.
        /// </summary>
        /// <param name="ex">The exception that caused the error.</param>
        /// <remarks>
        /// Automatically sets IsError to true and stores the exception.
        /// </remarks>
        public void SetError(Exception ex)
        {
            this.Exception = ex;
            this.IsError = true;
        }

        /// <summary>
        /// Configures embedded message parameters with optional overrides.
        /// </summary>
        /// <param name="ImageLink">Optional image URL for embed.</param>
        /// <param name="ThumbnailLink">Optional thumbnail URL for embed.</param>
        /// <param name="Footer">Optional footer text for embed.</param>
        /// <param name="Title">Optional title for embed.</param>
        /// <param name="Description">Optional description for embed.</param>
        /// <param name="Author">Optional author name for embed.</param>
        /// <remarks>
        /// Only non-empty strings will update the current values.
        /// Used to build rich embed responses for supported platforms.
        /// </remarks>
        public void SetEmbed(string ImageLink = "", string ThumbnailLink = "", string Footer = "", string Title = "", string Description = "", string Author = "")
        {
            if (ImageLink != "") this.ImageLink = ImageLink;
            if (ThumbnailLink != "") this.ThumbnailLink = ThumbnailLink;
            if (Footer != "") this.Footer = Footer;
            if (Title != "") this.Title = Title;
            if (Description != "") this.Description = Description;
            if (Author != "") this.Author = Author;
        }

        /// <summary>
        /// Sets nickname color and updates embed color accordingly.
        /// </summary>
        /// <param name="NicknameColor">The color preset for username display.</param>
        /// <remarks>
        /// Updates both BotNameColor and EmbedColor properties.
        /// EmbedColor uses Twitch-specific color mappings.
        /// </remarks>
        public void SetColor(ChatColorPresets NicknameColor)
        {
            this.BotNameColor = NicknameColor;
            this.EmbedColor = TwitchColors(NicknameColor);
        }

        /// <summary>
        /// Sets whether to use embed formatting for the response.
        /// </summary>
        /// <param name="IsEmbed">True to use embed format, false otherwise.</param>
        public void SetEmbed(bool IsEmbed)
        {
            this.IsEmbed = IsEmbed;
        }

        /// <summary>
        /// Sets whether the response should be ephemeral (visible only to sender).
        /// </summary>
        /// <param name="IsEphemeral">True for ephemeral messages, false for public.</param>
        public void SetEphemeral(bool IsEphemeral)
        {
            this.IsEphemeral = IsEphemeral;
        }

        /// <summary>
        /// Sets whether to bypass banword filtering for this message.
        /// </summary>
        /// <param name="IsSafe">True to bypass filtering, false for standard checks.</param>
        public void SetSafe(bool IsSafe)
        {
            this.IsSafe = IsSafe;
        }

        /// <summary>
        /// Gets the message content for this command response.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets whether to bypass banword filtering for this message.
        /// </summary>
        public bool IsSafe { get; private set; }

        /// <summary>
        /// Gets the description text for embedded messages.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the author name for embedded messages.
        /// </summary>
        public string Author { get; private set; }

        /// <summary>
        /// Gets the image URL for embedded messages.
        /// </summary>
        public string ImageLink { get; private set; }

        /// <summary>
        /// Gets the thumbnail URL for embedded messages.
        /// </summary>
        public string ThumbnailLink { get; private set; }

        /// <summary>
        /// Gets the footer text for embedded messages.
        /// </summary>
        public string Footer { get; private set; }

        /// <summary>
        /// Gets whether this response should be formatted as an embed.
        /// </summary>
        public bool IsEmbed { get; private set; }

        /// <summary>
        /// Gets whether this response should be ephemeral (private to sender).
        /// </summary>
        public bool IsEphemeral { get; private set; }

        /// <summary>
        /// Gets the title text for embedded messages.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the color for embedded messages.
        /// </summary>
        public System.Drawing.Color EmbedColor { get; private set; }

        /// <summary>
        /// Gets the color preset for bot username display.
        /// </summary>
        public ChatColorPresets BotNameColor { get; private set; }

        /// <summary>
        /// Gets whether this response represents an error condition.
        /// </summary>
        public bool IsError { get; private set; }

        /// <summary>
        /// Gets the exception associated with this error response, if any.
        /// </summary>
        public Exception? Exception { get; private set; }
    }
}
