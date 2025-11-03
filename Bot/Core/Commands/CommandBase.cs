using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands
{
    public abstract class CommandBase : ICommand
    {
        public abstract string Name { get; }
        public abstract string Author { get; }
        public abstract string Source { get; }
        public abstract Dictionary<Language, string> Description { get; }
        public abstract int UserCooldown { get; }
        public abstract int Cooldown { get; }
        public abstract string[] Aliases { get; }
        public abstract string Help { get; }
        public abstract DateTime CreationDate { get; }
        public abstract Roles RoleRequired { get; }
        public abstract Platform[] Platforms { get; }
        public abstract bool IsAsync { get; }
        public virtual bool TechWorks { get; } = false;

        public virtual CommandReturn Execute(CommandData data)
        {
            throw new NotImplementedException();
        }
        public virtual Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            throw new NotImplementedException();
        }
    }
}
