using Discord;
using System.Diagnostics;
using butterBror.Utils;
using TwitchLib.Client.Enums;
using butterBror;

namespace butterBror
{
    public partial class Commands
    {
        public class Status
        {
            public static CommandInfo Info = new()
            {
                name = "Status",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    { "ru", "MrDestructoid БО-Т НЕ РАБ-ОТАЕТ... НЕТ, Я СЕР-ЬЕЗНО!" },
                    { "en", "MrDestructoid THE BO-T DOES-N'T WORK... NO, I'M SER-IOUS!" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=status",
                cooldown_per_user = 20,
                cooldown_global = 10,
                aliases = ["status", "stat", "статус", "стат"],
                arguments = string.Empty,
                cooldown_reset = true,
                creation_date = DateTime.Parse("07/04/2024"),
                is_for_bot_moderator = true,
                is_for_bot_developer = true,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            // #CMD 1A
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string resultMessage = "";
                    string resultMessageTitle = "";

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

                    if (data.platform == Platforms.Twitch)
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
                    else if (data.platform == Platforms.Discord)
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

                    DirectoryInfo directory_info = new DirectoryInfo(Maintenance.path_main);
                    long folder_size = directory_info.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                    long folder_size_MB = folder_size / (1024 * 1024);
                    long folder_size_GB = folder_size / (1024 * 1024 * 1024);
                    int percent_folder_disk_used = 100 - (int)(float)(100.0 / diskSpace * folder_size_GB);

                    if (data.platform == Platforms.Twitch)
                    {
                        resultMessage = $"glorp 📡 Пшшш... Я butterBror v.{Engine.version} 💻 Статус: {statusName} 💾 Свободное место на диске ( {diskName.Replace("\\", "")} ): {avalibeDiskSpace} GB/{diskSpace} GB ({percentDiskUsed}% свободно) 🫙 Использовано оперативной памяти ботом: {workingAppSet} MB ⚖️ Вес базы данных бота: {folder_size_MB} MB/{diskSpace} GB ({percent_folder_disk_used}% свободно)";
                    }
                    else if (data.platform == Platforms.Discord)
                    {
                        resultMessageTitle = "📃 Статус бота";
                        resultMessage = $"<:OFFLINECHAT:1248250625754398730> 📡 Пшшш... Я butterBror v.{Engine.version} 💻 Статус: {statusName} 💾 Свободное место на диске ( {diskName.Replace("\\", "")} ): {avalibeDiskSpace} GB/{diskSpace} GB ({percentDiskUsed}% свободно) 🫙 Использовано оперативной памяти ботом: {workingAppSet} MB ⚖️ Вес базы данных бота: {folder_size_MB} MB/{diskSpace} GB ({percent_folder_disk_used}% свободно)";
                    }
                    return new()
                    {
                        message = resultMessage,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = resultMessageTitle,
                        embed_color = Color.Blue,
                        nickname_color = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
        }
    }
}
