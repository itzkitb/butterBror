namespace bb.Models.Users
{
    public class UserData
    {
        public required string Id { get; set; }
        public required string Language { get; set; }
        public required string Name { get; set; }
        public long? Balance { get; set; }
        public long? BalanceFloat { get; set; }
        public long? TotalMessages { get; set; }
        public bool? IsBanned { get; set; }
        public bool? Ignored { get; set; }
        public bool? IsModerator { get; set; }
        public bool? IsBroadcaster { get; set; }
        public bool? IsBotModerator { get; set; }
        public bool? IsBotDeveloper { get; set; }
    }
}
