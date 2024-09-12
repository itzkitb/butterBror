using Discord;
using System.Diagnostics;
using static butterBror.BotWorker;
using static butterBror.BotWorker.FileMng;
using butterBib;
using Discord.Rest;

namespace butterBror
{
    public partial class Commands
    {
        public class Status
        {
            public static CommandInfo Info = new()
            {
                Name = "Status",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды вы можете узнать статус бота.",
                UseURL = "NONE",
                UserCooldown = 20,
                GlobalCooldown = 10,
                aliases = ["status", "stat", "статус", "стат"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = true,
                ForBotCreator = true,
                ForChannelAdmins = false,
            };
            // #CMD 1A
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    string resultMessageTitle = "";


                    int status = 0;
                    string statusName = "";
                    // Оставшееся место на диске
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string driveLetter = Path.GetPathRoot(appDataPath);
                    DriveInfo driveInfo = new(driveLetter.Substring(0, 1));
                    long avalibeDiskSpace = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024); // GB
                    long diskSpace = driveInfo.TotalSize / (1024 * 1024 * 1024);
                    int percentDiskUsed = (int)(float)(100.0 / Tools.ToNumber(diskSpace.ToString()) * Tools.ToNumber(avalibeDiskSpace.ToString()));

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

                    // Оперативная память, занимаемая процессом
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

                    if (data.Platform == Platforms.Twitch)
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
                    else if (data.Platform == Platforms.Discord)
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

                    // Вес папки
                    string folderPath = $"{Bot.MainPath}";
                    DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                    long folderSize = dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length) / (1024 * 1024); // MB
                    long folderSizeGB = dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length) / (1024 * 1024 * 1024); // ГБ
                    int percentFolderDiskUsed = 100 - (int)(float)(100.0 / diskSpace * folderSizeGB);

                    if (data.Platform == Platforms.Twitch)
                    {
                        resultMessage = $"glorp 📡 Пшшш... Я butterBror v.{BotEngine.botVersion} 💻 Статус: {statusName} 💾 Свободное место на диске ( {diskName.Replace("\\", "")} ): {avalibeDiskSpace} GB/{diskSpace} GB ({percentDiskUsed}% свободно) 🫙 Использовано оперативной памяти ботом: {workingAppSet} MB ⚖️ Вес базы данных бота: {folderSize} MB/{diskSpace} GB ({percentFolderDiskUsed}% свободно)";
                    }
                    else if (data.Platform == Platforms.Discord)
                    {
                        resultMessageTitle = "📃 Статус бота";
                        resultMessage = $"<:OFFLINECHAT:1248250625754398730> 📡 Пшшш... Я butterBror v.{BotEngine.botVersion} 💻 Статус: {statusName} 💾 Свободное место на диске ( {diskName.Replace("\\", "")} ): {avalibeDiskSpace} GB/{diskSpace} GB ({percentDiskUsed}% свободно) 🫙 Использовано оперативной памяти ботом: {workingAppSet} MB ⚖️ Вес базы данных бота: {folderSize} MB/{diskSpace} GB ({percentFolderDiskUsed}% свободно)";
                    }
                    return new()
                    {
                        Message = resultMessage,
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = resultMessageTitle,
                        Color = Color.Blue,
                        NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
                    };
                }
                catch (Exception ex)
                {
                    Tools.ErrorOccured(ex.Message, "cmd1A");
                }
                return new()
                {
                    Message = TranslationManager.GetTranslation(data.User.Lang, "error", data.ChannelID),
                    IsSafeExecute = true,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = false,
                    Ephemeral = false,
                    Title = "",
                    Color = Color.Red,
                    NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.Red
                };
            }
        }
    }
}
