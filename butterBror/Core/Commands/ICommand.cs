using butterBror.Models;

namespace butterBror.Core.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Author { get; }
        string AuthorsGithub { get; }
        string GithubSource { get; }
        Version Version { get; }
        Dictionary<string, string> Description { get; }
        string WikiLink { get; }
        int CooldownPerUser { get; }
        int CooldownPerChannel { get; }
        string[] Aliases { get; }
        string HelpArguments { get; }
        DateTime CreationDate { get; }
        bool OnlyBotModerator { get; }
        bool OnlyBotDeveloper { get; }
        bool OnlyChannelModerator { get; }
        PlatformsEnum[] Platforms { get; }
        bool IsAsync { get; }

        CommandReturn Execute(CommandData data);
        Task<CommandReturn> ExecuteAsync(CommandData data);
    }
}
