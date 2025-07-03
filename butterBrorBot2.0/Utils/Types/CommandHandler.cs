using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types
{
    public class CommandHandler
    {
        public CommandInfo info { get; set; }
        public Func<CommandData, CommandReturn> sync_executor { get; set; }
        public Func<CommandData, Task<CommandReturn>> async_executor { get; set; }
    }
}
