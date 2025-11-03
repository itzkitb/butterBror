using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Author { get; }
        string Source { get; }
        Dictionary<Language, string> Description { get; }
        int UserCooldown { get; }
        int Cooldown { get; }
        string[] Aliases { get; }
        string Help { get; }
        DateTime CreationDate { get; }
        Roles RoleRequired { get; }
        Platform[] Platforms { get; }
        bool IsAsync { get; }
        bool TechWorks { get; }

        CommandReturn Execute(CommandData data);
        Task<CommandReturn> ExecuteAsync(CommandData data);
    }
}
