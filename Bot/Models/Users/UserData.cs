namespace bb.Models.Users
{
    public class UserData
    {
        public required string Id { get; set; }
        public required Language Language { get; set; }
        public required string Name { get; set; }
        public required decimal? Balance { get; set; }
        public long? TotalMessages { get; set; }
        public required Roles Roles { get; set; }
        public string BlockReason { get; set; }
    }
}
