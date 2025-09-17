using bb.Core.Commands;

namespace bb.Models
{
    public class CommandHandler
    {
        public CommandInfo Info { get; set; }
        public Func<CommandData, CommandReturn> SyncExecutor { get; set; }
        public Func<CommandData, Task<CommandReturn>> AsyncExecutor { get; set; }
    }
}
