using bb.Data.Repositories;

namespace bb.Core.Services
{
    public class SQLService
    {
        public ChannelsRepository Channels;
        public GamesRepository Games;
        public MessagesRepository Messages;
        public UsersRepository Users;
        public RolesRepository Roles;
    }
}
