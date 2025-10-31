using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using System.Diagnostics;

namespace bb.Core.Commands.List
{
    public class BotStatus : CommandBase
    {
        public override string Name => "BotStatus";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/BotStatus.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "MrDestructoid БО-Т НЕ РАБ-ОТАЕТ... НЕТ, Я СЕР-ЬЕЗНО!" },
            { Language.EnUs, "MrDestructoid THE BO-T DOES-N'T WORK... NO, I'M SER-IOUS!" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=botstatus";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["botstatus", "bstat", "ботстатус", "бстат"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override bool OnlyBotModerator => true;
        public override bool OnlyBotDeveloper => true;
        public override bool OnlyChannelModerator => false;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string driveLetter = Path.GetPathRoot(appDataPath).Substring(0, 1);
                DriveInfo driveInfo = new DriveInfo(driveLetter);

                long totalDiskBytes = driveInfo.TotalSize;
                long freeDiskBytes = driveInfo.AvailableFreeSpace;
                double percentFreeDisk = totalDiskBytes > 0
                    ? (freeDiskBytes * 100.0) / totalDiskBytes
                    : 0;
                int diskStatus = CalculateDiskStatus(percentFreeDisk);

                Process process = Process.GetCurrentProcess();
                long workingSetMB = process.WorkingSet64 / (1024 * 1024);
                int memoryStatus = CalculateMemoryStatus(workingSetMB);

                int generalStatus = Math.Min(diskStatus, memoryStatus);

                string statusName = GetStatusName(generalStatus);

                DirectoryInfo directoryInfo = new DirectoryInfo(bb.Program.BotInstance.Paths.General);
                long folderSizeBytes = directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories)
                    .Sum(fi => fi.Length);
                long folderSizeMB = folderSizeBytes / (1024 * 1024);
                double diskUsagePercent = totalDiskBytes > 0
                    ? (folderSizeBytes * 100.0) / totalDiskBytes
                    : 0;

                string prefix = data.Platform switch
                {
                    Platform.Twitch => "glorp",
                    Platform.Discord => "<:OFFLINECHAT:1248250625754398730>",
                    _ => ""
                };

                string diskName = driveLetter + ":";
                string message = $"{prefix} 📡 | Pshhh... I'm ButterBror v.{bb.Program.BotInstance.Version} • " +
                                 $"Status: {statusName} • " +
                                 $"Free disk space ({diskName}): {FormatSize(freeDiskBytes)}/{FormatSize(totalDiskBytes)} " +
                                 $"({Math.Round(percentFreeDisk)}% free) • " +
                                 $"Working memory: {workingSetMB} MB • " +
                                 $"Database size: {folderSizeMB} MB ({Math.Round(diskUsagePercent)}% of disk)";

                commandReturn.SetMessage(message);
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }

        private int CalculateDiskStatus(double freePercent)
        {
            if (freePercent > 85) return 5;
            if (freePercent > 50) return 4;
            if (freePercent > 20) return 3;
            if (freePercent > 10) return 2;
            return 1;
        }

        private int CalculateMemoryStatus(long workingSetMB)
        {
            if (workingSetMB < 100) return 5;
            if (workingSetMB < 250) return 4;
            if (workingSetMB < 500) return 3;
            if (workingSetMB < 1000) return 2;
            return 1;
        }

        private string GetStatusName(int status)
        {
            return status switch
            {
                5 => "Perfect",
                4 => "Great",
                3 => "Normal",
                2 => "Bad",
                1 => "Very bad",
                _ => "AINTNOWAY"
            };
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):0.0} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):0.0} GB";
        }
    }
}
