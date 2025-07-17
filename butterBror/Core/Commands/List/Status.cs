using butterBror.Models;
using butterBror.Core.Bot;
using System.Diagnostics;

namespace butterBror.Core.Commands.List
{
    public class Status : CommandBase
    {
        public override string Name => "Status";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Status.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "MrDestructoid БО-Т НЕ РАБ-ОТАЕТ... НЕТ, Я СЕР-ЬЕЗНО!" },
            { "en", "MrDestructoid THE BO-T DOES-N'T WORK... NO, I'M SER-IOUS!" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=status";
        public override int CooldownPerUser => 20;
        public override int CooldownPerChannel => 10;
        public override string[] Aliases => ["status", "stat", "статус", "стат"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => true;
        public override bool OnlyBotDeveloper => true;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                int status = 0;
                string statusName = "";
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string driveLetter = Path.GetPathRoot(appDataPath);
                DriveInfo driveInfo = new(driveLetter.Substring(0, 1));
                long avalibeDiskSpace = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024); // GB
                long diskSpace = driveInfo.TotalSize / (1024 * 1024 * 1024);
                int percentDiskUsed = (int)(float)(100.0 / Utils.Format.ToInt(diskSpace.ToString()) * Utils.Format.ToInt(avalibeDiskSpace.ToString()));

                if (percentDiskUsed > 80)
                {
                    status += 3;
                }
                else if (percentDiskUsed > 50)
                {
                    status += 2;
                }
                else if (percentDiskUsed > 15)
                {
                    status += 1;
                }

                string diskName = driveInfo.Name;

                Process process = Process.GetCurrentProcess();
                long workingAppSet = process.WorkingSet64 / (1024 * 1024); // MB

                if (workingAppSet < 100)
                {
                    status += 4;
                }
                else if (workingAppSet < 250)
                {
                    status += 3;
                }
                else if (workingAppSet < 500)
                {
                    status += 2;
                }
                else if (workingAppSet < 1000)
                {
                    status += 1;
                }

                if (data.Platform == PlatformsEnum.Twitch)
                {
                    if (status >= 6)
                    {
                        statusName = "catWOW Прекрасно";
                    }
                    else if (status >= 4)
                    {
                        statusName = "Klass Отлично";
                    }
                    else if (status >= 3)
                    {
                        statusName = ":/ Нормально";
                    }
                    else if (status >= 2)
                    {
                        statusName = "monka Плохо";
                    }
                    else if (status >= 1)
                    {
                        statusName = "forsenAgony Ужасно";
                    }
                    else
                    {
                        statusName = "AINTNOWAY";
                    }
                }
                else if (data.Platform == PlatformsEnum.Discord)
                {
                    if (status >= 6)
                    {
                        statusName = "<:peepoLove:1248250622889951346> Прекрасно";
                    }
                    else if (status >= 4)
                    {
                        statusName = "<:ApuScience:1248250603906535454> Отлично";
                    }
                    else if (status >= 3)
                    {
                        statusName = "<:Sadge:1248250606741884941> Нормально";
                    }
                    else if (status >= 2)
                    {
                        statusName = "<:peepoWtf:1248250614841081907> Плохо";
                    }
                    else if (status >= 1)
                    {
                        statusName = "<:PepeA:1248250633178579036> Ужасно";
                    }
                    else
                    {
                        statusName = "☠";
                    }
                }

                DirectoryInfo directory_info = new DirectoryInfo(Engine.Bot.Pathes.Main);
                long folder_size = directory_info.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                long folder_size_MB = folder_size / (1024 * 1024);
                long folder_size_GB = folder_size / (1024 * 1024 * 1024);
                int percent_folder_disk_used = 100 - (int)(float)(100.0 / diskSpace * folder_size_GB);

                if (data.Platform == PlatformsEnum.Twitch)
                {
                    commandReturn.SetMessage($"glorp 📡 Pshhh... I'm ButterBror v.{Engine.Version} 💻 Status: {statusName} 💾 Free disk space ( {diskName.Replace("\\", "")} ): {avalibeDiskSpace} GB/{diskSpace} GB ({percentDiskUsed}% free) 🫙 Used working memory by bot: {workingAppSet} MB ⚖️ Bot database weight: {folder_size_MB} MB/{diskSpace} GB ({percent_folder_disk_used}% free)");
                }
                else if (data.Platform == PlatformsEnum.Discord)
                {
                    commandReturn.SetMessage($"<:OFFLINECHAT:1248250625754398730> 📡 Pshhh... I'm ButterBror v.{Engine.Version} 💻 Status: {statusName} 💾 Free disk space ( {diskName.Replace("\\", "")} ): {avalibeDiskSpace} GB/{diskSpace} GB ({percentDiskUsed}% free) 🫙 Used working memory by bot: {workingAppSet} MB ⚖️ Bot database weight: {folder_size_MB} MB/{diskSpace} GB ({percent_folder_disk_used}% free)");
                }
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
