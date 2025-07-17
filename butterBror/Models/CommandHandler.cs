using butterBror.Core.Commands;

namespace butterBror.Models
{
    public class CommandHandler
    {
        public CommandInfo Info { get; set; }
        public Func<CommandData, CommandReturn> sync_executor { get; set; }
        public Func<CommandData, Task<CommandReturn>> async_executor { get; set; }
    }
}
