using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Core.Bot.SQLColumnNames
{
    public class Users
    {
        public const string ID = "ID";
        public const string FirstMessage = "FirstMessage";
        public const string FirstSeen = "FirstSeen";
        public const string FirstChannel = "FirstChannel";
        public const string LastMessage = "LastMessage";
        public const string LastSeen = "LastSeen";
        public const string LastChannel = "LastChannel";
        public const string Balance = "Balance";
        public const string AfterDotBalance = "AfterDotBalance";
        public const string Rating = "Rating";
        public const string IsAFK = "IsAFK";
        public const string AFKText = "AFKText";
        public const string AFKType = "AFKType";
        public const string Reminders = "Reminders";
        public const string LastCookie = "LastCookie";
        public const string GiftedCookies = "GiftedCookies";
        public const string EatedCookies = "EatedCookies";
        public const string BuyedCookies = "BuyedCookies";
        public const string ReceivedCookies = "ReceivedCookies";
        public const string Location = "Location";
        public const string Longitude = "Longitude";
        public const string Latitude = "Latitude";
        public const string Language = "Language";
        public const string AFKStart = "AFKStart";
        public const string AFKResume = "AFKResume";
        public const string AFKResumeTimes = "AFKResumeTimes";
        public const string LastUse = "LastUse";
        public const string GPTHistory = "GPTHistory";
        public const string WeatherResultLocations = "WeatherResultLocations";
        public const string TotalMessages = "TotalMessages";
        public const string TotalMessagesLength = "TotalMessagesLength";
        public const string ChannelMessagesCount = "ChannelMessagesCount";
    }

    public class UsernameMapping
    {
        public const string Platform = "Platform";
        public const string UserID = "UserID";
        public const string Username = "Username";
    }

    public class Messages
    {
        public const string ID = "ID";
        public const string UserID = "UserID";
        public const string MessageDate = "MessageDate";
        public const string MessageText = "MessageText";
        public const string IsMe = "IsMe";
        public const string IsModerator = "IsModerator";
        public const string IsSubscriber = "IsSubscriber";
        public const string IsPartner = "IsPartner";
        public const string IsStaff = "IsStaff";
        public const string IsTurbo = "IsTurbo";
        public const string IsVip = "IsVip";
    }

    public class Cookies
    {
        public const string Platform = "Platform";
        public const string UserID = "UserID";
        public const string EatersCount = "EatersCount";
        public const string GiftersCount = "GiftersCount";
        public const string RecipientsCount = "RecipientsCount";
    }

    public class Frogs
    {
        public const string Platform = "Platform";
        public const string UserID = "UserID";
        public const string _Frogs = "Frogs";
        public const string Gifted = "Gifted";
        public const string Received = "ID";
    }

    public class Channels
    {
        public const string ChannelID = "ID";
        public const string CDDData = "ID";
        public const string BanWords = "ID";
    }

    public class FirstMessage
    {
        public const string ChannelID = "ID";
        public const string UserID = "UserID";
        public const string MessageDate = "MessageDate";
        public const string MessageText = "MessageText";
        public const string IsMe = "IsMe";
        public const string IsModerator = "IsModerator";
        public const string IsSubscriber = "IsSubscriber";
        public const string IsPartner = "IsPartner";
        public const string IsStaff = "IsStaff";
        public const string IsTurbo = "IsTurbo";
        public const string IsVip = "IsVip";
    }
}
