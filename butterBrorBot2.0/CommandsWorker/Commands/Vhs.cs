using butterBib;
using TwitchLib.Client.Enums;
using butterBror.Utils;

namespace butterBror
{
    public partial class Commands
    {
        public class Vhs
        {
            public static CommandInfo Info = new()
            {
                Name = "Vhs",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "0K8g0L1O0YcwzLXQs82i0L7NnyDQvc2gMyDQss2P0LjQtjQuzLggzLYxzaFZ0YLMmyDRgdC7Ts2c0YhrMNKJ0LzNmCDRgs2YZU3NmEjQvi7NniDNgdCiYnwgONKJ0LjNnzTQuMy20YhizaAg0LwzzYDQvcy00Y8/zKg=",
                UseURL = "NONE",
                UserCooldown = 60,
                GlobalCooldown = 10,
                aliases = ["cassette", "vhs", "foundfootage", "footage"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                if (CommandUtil.IsNotOnCooldown(3600, 1, "VhsReset", data.UserUUID, data.ChannelID, false))
                {
                    Task task = Task.Run(() =>
                    {
                        Random rand = new Random();
                        if (data.Platform == Platforms.Twitch)
                        {
                            Thread.Sleep(rand.Next(10000, 30000));
                        }
                        var videos = YTUtil.GetPlaylistVideos("https://www.youtube.com/playlist?list=PLAZUCud8HyO-9Ni4BSFkuBTOK8e3S5OLL");
                        Random rand2 = new Random();
                        int index = rand2.Next(videos.Length);
                        string randomUrl = videos[index];
                        if (data.Platform == Platforms.Twitch)
                        {
                            TwitchMessageSendData SendData = new()
                            {
                                Message = TranslationManager.GetTranslation(data.User.Lang, "vhsResult", data.ChannelID).Replace("%url%", randomUrl),
                                Channel = data.Channel,
                                ChannelID = data.ChannelID,
                                AnswerID = data.TWargs.Command.ChatMessage.Id,
                                Lang = data.User.Lang,
                                Name = data.User.Name,
                                IsSafeExecute = true,
                                NickNameColor = ChatColorPresets.YellowGreen
                            };
                            
                            butterBib.Commands.SendCommandReply(SendData);
                        }
                        else if (data.Platform == Platforms.Discord)
                        {
                            DiscordCommandSendData SendData = new()
                            {
                                Message = TranslationManager.GetTranslation(data.User.Lang, "vhsResult", data.ChannelID).Replace("%url%", randomUrl),
                                Description = "",
                                IsEmbed = false,
                                Ephemeral = false,
                                Server = data.Channel,
                                ServerID = data.ChannelID,
                                Lang = data.User.Lang,
                                IsSafeExecute = true,
                                d = data.d
                            };
                            butterBib.Commands.SendCommandReply(SendData);
                        }
                    });
                    return new()
                    {
                        Message = TranslationManager.GetTranslation(data.User.Lang, "vhsSearch", data.ChannelID),
                        IsSafeExecute = true,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = "",
                        Color = Discord.Color.Blue,
                        NickNameColor = ChatColorPresets.DodgerBlue
                    };
                }
                else
                {
                    return new()
                    {
                        Message = TranslationManager.GetTranslation(data.User.Lang, "vhsWait", data.ChannelID),
                        IsSafeExecute = true,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = "",
                        Color = Discord.Color.Red,
                        NickNameColor = ChatColorPresets.Red
                    };
                }
            }
        }
    }
}
