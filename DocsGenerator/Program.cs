using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CommandDocsGenerator
{
    public class CommandInfo
    {
        public string Name { get; set; }
        public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();
        public string[] Aliases { get; set; } = Array.Empty<string>();
        public string WikiLink { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
        public int CooldownPerUser { get; set; }
        public int CooldownPerChannel { get; set; }
        public bool OnlyBotModerator { get; set; }
        public bool OnlyBotDeveloper { get; set; }
        public bool OnlyChannelModerator { get; set; }
        public string HelpArguments { get; set; } = string.Empty;
        public string[] Platforms { get; set; } = Array.Empty<string>();
    }

    class Program
    {
        static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["title"] = new()
            {
                ["ru-RU"] = "Список команд",
                ["en-US"] = "Command List"
            },
            ["command_title"] = new()
            {
                ["ru-RU"] = "Команда",
                ["en-US"] = "Command"
            },
            ["description"] = new()
            {
                ["ru-RU"] = "Описание",
                ["en-US"] = "Description"
            },
            ["aliases"] = new()
            {
                ["ru-RU"] = "Алиасы",
                ["en-US"] = "Aliases"
            },
            ["usage"] = new()
            {
                ["ru-RU"] = "Использование",
                ["en-US"] = "Usage"
            },
            ["cooldown"] = new()
            {
                ["ru-RU"] = "Кулдаун",
                ["en-US"] = "Cooldown"
            },
            ["user_cooldown"] = new()
            {
                ["ru-RU"] = "Пользователь",
                ["en-US"] = "User"
            },
            ["channel_cooldown"] = new()
            {
                ["ru-RU"] = "Канал",
                ["en-US"] = "Channel"
            },
            ["permissions"] = new()
            {
                ["ru-RU"] = "Права доступа",
                ["en-US"] = "Permissions"
            },
            ["bot_mod"] = new()
            {
                ["ru-RU"] = "Модератор бота",
                ["en-US"] = "Bot Moderator"
            },
            ["bot_dev"] = new()
            {
                ["ru-RU"] = "Разработчик бота",
                ["en-US"] = "Bot Developer"
            },
            ["channel_mod"] = new()
            {
                ["ru-RU"] = "Модератор канала",
                ["en-US"] = "Channel Moderator"
            },
            ["platforms"] = new()
            {
                ["ru-RU"] = "Платформы",
                ["en-US"] = "Platforms"
            },
            ["version"] = new()
            {
                ["ru-RU"] = "Версия",
                ["en-US"] = "Version"
            },
            ["created"] = new()
            {
                ["ru-RU"] = "Дата создания",
                ["en-US"] = "Created"
            },
            ["wiki"] = new()
            {
                ["ru-RU"] = "Wiki",
                ["en-US"] = "Wiki"
            },
            ["yes"] = new()
            {
                ["ru-RU"] = "Да",
                ["en-US"] = "Yes"
            },
            ["no"] = new()
            {
                ["ru-RU"] = "Нет",
                ["en-US"] = "No"
            },
            ["example"] = new()
            {
                ["ru-RU"] = "Пример",
                ["en-US"] = "Example"
            },
            ["property"] = new()
            {
                ["ru-RU"] = "Свойство",
                ["en-US"] = "Property"
            },
            ["value"] = new()
            {
                ["ru-RU"] = "Значение",
                ["en-US"] = "Value"
            },
            ["all_platforms"] = new()
            {
                ["ru-RU"] = "Все платформы",
                ["en-US"] = "All platforms"
            }
        };

        public static string Executor = "_";

        static void Main(string[] args)
        {
            string commandsDirectory = @"..\..\..\..\Bot\Core\Commands\List";
            string outputBaseDirectory = @"bb_docs\docs";

            Console.WriteLine("Search command in: " + Path.GetFullPath(commandsDirectory));
            var commands = FindAndParseCommands(commandsDirectory);

            Console.WriteLine($"Commands found: {commands.Count}");
            foreach (var cmd in commands)
            {
                Console.WriteLine($"- {cmd.Name} (алиасы: {string.Join(", ", cmd.Aliases)})");
            }

            GenerateDocumentation(commands, outputBaseDirectory);
            Console.WriteLine("Documentation successfully generated!");
        }

        static List<CommandInfo> FindAndParseCommands(string directoryPath)
        {
            var commands = new List<CommandInfo>();
            var files = Directory.GetFiles(directoryPath, "*.cs");

            foreach (var file in files)
            {
                var command = ParseCommandFile(file);
                if (command != null)
                {
                    commands.Add(command);
                }
            }

            return commands;
        }

        static CommandInfo ParseCommandFile(string filePath)
        {
            string content = File.ReadAllText(filePath);

            if (!Regex.IsMatch(content, @"\bpublic\s+class\s+\w+\s*:\s*CommandBase\b"))
                return null;

            var command = new CommandInfo();

            var nameMatch = Regex.Match(content, @"public\s+override\s+string\s+Name\s*=>\s*""([^""]+)""");
            if (!nameMatch.Success) return null;
            command.Name = nameMatch.Groups[1].Value;

            var argumentsMatch = Regex.Match(content, @"public\s+override\s+string\s+HelpArguments\s*=>\s*""([^""]+)""");
            if (argumentsMatch.Success) command.HelpArguments = argumentsMatch.Groups[1].Value;

            var descMatch = Regex.Match(content,
                @"public\s+override\s+Dictionary<string,\s*string>\s+Description\s*=>\s*new\(\)\s*\{([\s\S]*?)\};",
                RegexOptions.Multiline);

            if (descMatch.Success)
            {
                var entries = Regex.Matches(descMatch.Groups[1].Value,
                    @"\{\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\}");

                foreach (Match entry in entries)
                {
                    command.Description[entry.Groups[1].Value.Trim()] =
                        entry.Groups[2].Value.Trim();
                }
            }

            var aliasesMatch = Regex.Match(content,
                @"public\s+override\s+string\[\]\s*Aliases\s*=>\s*\[([^\]]+)\];");

            if (aliasesMatch.Success)
            {
                var aliasMatches = Regex.Matches(aliasesMatch.Groups[1].Value,
                    @"""([^""]+)""");

                command.Aliases = new string[aliasMatches.Count];
                for (int i = 0; i < aliasMatches.Count; i++)
                {
                    command.Aliases[i] = aliasMatches[i].Groups[1].Value;
                }
            }

            var wikiMatch = Regex.Match(content,
                @"public\s+override\s+string\s+WikiLink\s*=>\s*""([^""]+)""");
            if (wikiMatch.Success)
                command.WikiLink = wikiMatch.Groups[1].Value.Trim();

            var versionMatch = Regex.Match(content,
                @"public\s+override\s+Version\s+Version\s*=>\s*new\s+Version\(""([^""]+)""\)");
            if (versionMatch.Success)
                command.Version = versionMatch.Groups[1].Value;

            var dateMatch = Regex.Match(content,
                @"public\s+override\s+DateTime\s+CreationDate\s*=>\s*DateTime\.Parse\(""([^""]+)""\)");
            if (dateMatch.Success)
                command.CreationDate = dateMatch.Groups[1].Value;

            var cooldownUserMatch = Regex.Match(content,
                @"public\s+override\s+int\s+CooldownPerUser\s*=>\s*(\d+)");
            if (cooldownUserMatch.Success)
                command.CooldownPerUser = int.Parse(cooldownUserMatch.Groups[1].Value);

            var cooldownChannelMatch = Regex.Match(content,
                @"public\s+override\s+int\s+CooldownPerChannel\s*=>\s*(\d+)");
            if (cooldownChannelMatch.Success)
                command.CooldownPerChannel = int.Parse(cooldownChannelMatch.Groups[1].Value);

            command.OnlyBotModerator = ContainsBooleanValue(content, "OnlyBotModerator", true);
            command.OnlyBotDeveloper = ContainsBooleanValue(content, "OnlyBotDeveloper", true);
            command.OnlyChannelModerator = ContainsBooleanValue(content, "OnlyChannelModerator", true);

            var platformsMatch = Regex.Match(content,
                @"public\s+override\s+PlatformsEnum\[\]\s+Platforms\s*=>\s*\[\s*([^\]]+)\s*\]");

            if (platformsMatch.Success)
            {
                command.Platforms = platformsMatch.Groups[1].Value
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim().Split('.').Last())
                    .ToArray();
            }

            return command;
        }

        static bool ContainsBooleanValue(string content, string propertyName, bool value)
        {
            var match = Regex.Match(content,
                $@"public\s+override\s+bool\s+{propertyName}\s*=>\s*{(value ? "true" : "false")}");
            return match.Success;
        }

        static void GenerateDocumentation(List<CommandInfo> commands, string outputBaseDirectory)
        {
            foreach (var lang in new[] { "ru-RU", "en-US" })
            {
                string langDir = Path.Combine(outputBaseDirectory, lang);
                string commandsDir = Path.Combine(langDir, "commands");
                Directory.CreateDirectory(commandsDir);

                GenerateCommandsList(commands, lang, langDir);

                foreach (var command in commands)
                {
                    GenerateCommandFile(command, lang, commandsDir);
                }
            }
        }

        static void GenerateCommandsList(List<CommandInfo> commands, string lang, string outputDir)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# {Translations["title"][lang]}");
            sb.AppendLine();
            sb.AppendLine("en-US" == lang
                ? "This is a comprehensive list of all available commands with their descriptions and aliases."
                : "Полный список всех доступных команд с описаниями и алиасами.");
            sb.AppendLine();

            sb.AppendLine("| " + Translations["command_title"][lang] + " | " + Translations["description"][lang] + " |");
            sb.AppendLine("|:----------------|:----------------|");

            foreach (var command in commands.OrderBy(c => c.Name))
            {
                if (command.Description.TryGetValue(lang, out var desc))
                {
                    string aliases = string.Join(", ", command.Aliases.Take(3));
                    if (command.Aliases.Length > 3) aliases += ", ...";

                    sb.AppendLine($"| **[{command.Name}](commands/{command.Name.ToLower()}.md)**<br>`{aliases}` | {desc} |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("en-US" == lang
                ? "_Use the command name or any of the aliases to execute the command._"
                : "_Для выполнения команды используйте её название или любой из алиасов._");

            File.WriteAllText(Path.Combine(outputDir, "commands.md"), sb.ToString());
        }

        static void GenerateCommandFile(CommandInfo command, string lang, string outputDir)
        {
            if (!command.Description.TryGetValue(lang, out var desc))
                return;

            string fileName = Path.Combine(outputDir, $"{command.Name.ToLower()}.md");
            var sb = new StringBuilder();

            sb.AppendLine($"# {command.Name}");
            sb.AppendLine();
            sb.AppendLine($"<span style=\"color: #666; font-style: italic;\">{desc}</span>");
            sb.AppendLine();

            sb.AppendLine("## ℹ️ " + Translations["description"][lang]);
            sb.AppendLine();

            if (!string.IsNullOrEmpty(command.HelpArguments))
            {
                string exampleCommand = command.Aliases.Length > 0 ? command.Aliases[0] : command.Name.ToLower();
                sb.AppendLine($"`{Executor}{exampleCommand} {command.HelpArguments}`");
                sb.AppendLine();
                sb.AppendLine($"**{Translations["example"][lang]}:** `{Executor}{exampleCommand} {GetExampleArguments(command.HelpArguments)}`");
                sb.AppendLine();
            }

            sb.AppendLine("## 📋 " + Translations["command_title"][lang] + " Info");
            sb.AppendLine();
            sb.AppendLine("| **" + Translations["property"][lang] + "** | **" + Translations["value"][lang] + "** |");
            sb.AppendLine("|:----------------|:----------------|");

            sb.AppendLine($"| **{Translations["aliases"][lang]}** | {string.Join(", ", command.Aliases)} |");

            string platforms = command.Platforms.Length > 0
                ? string.Join(", ", command.Platforms)
                : Translations["all_platforms"][lang];
            sb.AppendLine($"| **{Translations["platforms"][lang]}** | {platforms} |");

            sb.AppendLine($"| **{Translations["cooldown"][lang]}** | - **{Translations["user_cooldown"][lang]}:** {command.CooldownPerUser} sec<br> - **{Translations["channel_cooldown"][lang]}:** {command.CooldownPerChannel} sec |");

            sb.AppendLine($"| **{Translations["permissions"][lang]}** | - **{Translations["bot_mod"][lang]}:** {GetYesNo(command.OnlyBotModerator, lang)}<br> - **{Translations["bot_dev"][lang]}:** {GetYesNo(command.OnlyBotDeveloper, lang)}<br> - **{Translations["channel_mod"][lang]}:** {GetYesNo(command.OnlyChannelModerator, lang)} |");

            sb.AppendLine($"| **{Translations["version"][lang]}** | {command.Version} |");
            sb.AppendLine($"| **{Translations["created"][lang]}** | {command.CreationDate} |");

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine($"💡 *{("en-US" == lang ? "Tip: Use the command with any of its aliases for faster access." : "Совет: Используйте команду любым из алиасов для более быстрого доступа.")}*");

            File.WriteAllText(fileName, sb.ToString());
        }

        static string GetYesNo(bool value, string lang)
        {
            return value
                ? Translations["yes"][lang]
                : Translations["no"][lang];
        }

        static string GetExampleArguments(string helpArguments)
        {
            return helpArguments
                .Replace("<", "")
                .Replace(">", "")
                .Replace("[", "")
                .Replace("]", "")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(arg => "example")
                .Aggregate((a, b) => a + " " + b);
        }
    }
}