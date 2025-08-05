using butterBror.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace butterBror.Core.Bot
{
    public class SQLWorker
    {
        public ChannelsDatabase Channels;
        public GamesDatabase Games;
        public MessagesDatabase Messages;
        public UsersDatabase Users;
        public RolesDatabase Roles;
    }
}
