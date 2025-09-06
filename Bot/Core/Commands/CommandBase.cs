using bb.Models;

namespace bb.Core.Commands
{
    public abstract class CommandBase : ICommand
    {
        public abstract string Name { get; }
        public abstract string Author { get; }
        public abstract string AuthorsGithub { get; }
        public abstract string GithubSource { get; }
        public abstract Version Version { get; }
        public abstract Dictionary<string, string> Description { get; }
        public abstract string WikiLink { get; }
        public abstract int CooldownPerUser { get; }
        public abstract int CooldownPerChannel { get; }
        public abstract string[] Aliases { get; }
        public abstract string HelpArguments { get; }
        public abstract DateTime CreationDate { get; }
        public abstract bool OnlyBotModerator { get; }
        public abstract bool OnlyBotDeveloper { get; }
        public abstract bool OnlyChannelModerator { get; }
        public abstract PlatformsEnum[] Platforms { get; }
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
