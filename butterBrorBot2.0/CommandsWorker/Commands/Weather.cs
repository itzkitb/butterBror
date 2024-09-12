using System.Text.RegularExpressions;
using TwitchLib.Client.Events;
using static butterBror.BotWorker;
using static butterBror.BotWorker.FileMng;
using Discord;
using Discord.WebSocket;
using butterBib;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Weather
        {
            public static CommandInfo Info = new()
            {
                Name = "Weather",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда помогает вам получить текущую погоду в каком-либо городе.",
                UseURL = "https://itzkitb.ru/bot_command/weather",
                UserCooldown = 10,
                GlobalCooldown = 5,
                aliases = ["weather", "погода", "wthr", "пгд", "пугода"],
                ArgsRequired = "(место)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string[] showAlias = ["show", "s", "показать", "п"];
                string[] pageAlias = ["page", "p", "страница", "с"];

                string? location = "";
                bool? isShow = false;
                bool? isPage = false;
                long? Page = 0;
                long? ShowID = 0;

                if (data.Platform == Platforms.Twitch)
                {
                    location = Tools.FilterText(data.ArgsAsString);
                    if (data.TWargs.Command.ArgumentsAsList.Count >= 2)
                    {
                        if (showAlias.Contains(data.TWargs.Command.ArgumentsAsList[0].ToLower().ToString()))
                        {
                            isShow = true;
                            ShowID = Tools.ToNumber(data.TWargs.Command.ArgumentsAsList[1].ToLower().ToString());
                        }
                        else if (pageAlias.Contains(data.TWargs.Command.ArgumentsAsList[0].ToLower().ToString()))
                        {
                            isPage = true;
                            Page = Tools.ToNumber(data.TWargs.Command.ArgumentsAsList[1].ToLower().ToString());
                        }
                    }
                }
                else if (data.Platform == Platforms.Discord)
                {
                    if (data.DSargs.ContainsKey("location"))
                    {
                        location = data.DSargs.GetValueOrDefault("location");
                    }
                    if (data.DSargs.ContainsKey("showpage"))
                    {
                        Page = data.DSargs.GetValueOrDefault("showpage");
                        isPage = true;
                    }
                    else if (data.DSargs.ContainsKey("page"))
                    {
                        ShowID = data.DSargs.GetValueOrDefault("page");
                        isShow = true;
                    }
                }
                string resultMessage = "";
                string resultMessageTitle = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                if ((bool)isPage)
                {
                    var weatherResultLocationsUnworked = UsersData.UserGetData<string[]>(data.UserUUID, "weatherResultLocations");
                    var weatherResultLocations = new List<Tools.Place>();

                    foreach (var data2 in weatherResultLocationsUnworked)
                    {
                        string pattern = @"name: ""(.*?)"", lat: ""(.*?)"", lon: ""(.*?)""";
                        Match match = Regex.Match(data2, pattern);

                        if (match.Success)
                        {
                            string name = match.Groups[1].Value;
                            string lat = match.Groups[2].Value;
                            string lon = match.Groups[3].Value;
                            Tools.Place placeData = new()
                            {
                                name = name,
                                lat = lat,
                                lon = lon
                            };
                            weatherResultLocations.Add(placeData);
                        }
                    }
                    if (weatherResultLocations != default)
                    {
                        int maxPages = (int)Math.Ceiling((double)(weatherResultLocations.Count / 5));
                        if (maxPages >= Page)
                        {
                            string locationPage = "";
                            int startID = (int)((Page * 5) - 4);
                            if (weatherResultLocations.Count > 5)
                            {
                                int index = startID;
                                for (int i = 0; i < 5; i++)
                                {
                                    locationPage += $"{index}. {weatherResultLocations[index - 1].name} (lat: {Tools.ShortenCoordinate(weatherResultLocations[index - 1].lat)}, lon: {Tools.ShortenCoordinate(weatherResultLocations[index - 1].lon)}), ";
                                    index++;
                                }
                                locationPage = locationPage.TrimEnd(',', ' ');
                            }
                            else
                            {
                                int index = startID;
                                foreach (var location2 in weatherResultLocations)
                                {
                                    index++;
                                    locationPage += $"{index}. {location2.name} (lat: {location2.lat}, lon: {location2.lon}), ";
                                }
                            }
                            locationPage = (locationPage + "\n").Replace(", \n", "");
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesShowedPlaces", data.ChannelID)
                                .Replace("%places%", locationPage)
                                .Replace("%page%", Page.ToString())
                                .Replace("%pages%", maxPages.ToString());
                            resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesDsTitle", data.ChannelID);
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesOverPage", data.ChannelID);
                            resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesDsTitle", data.ChannelID);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesNoPages", data.ChannelID);
                        resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesDsTitle", data.ChannelID);
                        resultColor = Color.Red;
                        resultNicknameColor = ChatColorPresets.Red;
                    }
                }
                else if ((bool)isShow)
                {
                    var weatherResultLocationsUnworked = UsersData.UserGetData<string[]>(data.UserUUID, "weatherResultLocations");
                    var weatherResultLocations = new List<Tools.Place>();
                    foreach (var data2 in weatherResultLocationsUnworked)
                    {
                        string pattern = @"name: ""(.*?)"", lat: ""(.*?)"", lon: ""(.*?)""";
                        Match match = Regex.Match(data2, pattern);

                        if (match.Success)
                        {
                            string name = match.Groups[1].Value;
                            string lat = match.Groups[2].Value;
                            string lon = match.Groups[3].Value;
                            Tools.Place placeData = new()
                            {
                                name = name,
                                lat = lat,
                                lon = lon
                            };
                            weatherResultLocations.Add(placeData);
                        }
                    }
                    if (weatherResultLocations?.Count > 1)
                    {
                        if (ShowID <= weatherResultLocations?.Count)
                        {
                            var weather = Tools.Get_weather(weatherResultLocations[(int)ShowID - 1].lat, weatherResultLocations[(int)ShowID - 1].lon);
                            var result = weather.Result.current;
                            if (result.temperature != -400)
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "weatherSucceful", data.ChannelID)
                                    .Replace("%emote%", Tools.GetWeatherEmoji(result.temperature))
                                    .Replace("%name%", weatherResultLocations[(int)ShowID - 1].name)
                                    .Replace("%temperature%", result.temperature.ToString())
                                    .Replace("%feelsLike%", result.feels_like.ToString())
                                    .Replace("%windSpeed%", result.wind.speed.ToString())
                                    .Replace("%summary%", Tools.GetWeatherSummary(data.User.Lang, result.summary.ToString(), data.ChannelID))
                                    .Replace("%pressure%", result.pressure.ToString())
                                    .Replace("%uvIndex%", result.uv_index.ToString())
                                    .Replace("%humidity%", result.humidity.ToString())
                                    .Replace("%visibility%", result.visibility.ToString())
                                    .Replace("%skyEmote%", Tools.GetWeatherSummaryEmoji(result.summary.ToString()));
                                resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsTitle", data.ChannelID);
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "placeCrack", data.ChannelID);
                                resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                                resultColor = Color.Red;
                                resultNicknameColor = ChatColorPresets.Red;
                            }
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesOverShowLocation", data.ChannelID);
                            resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesNoPages", data.ChannelID);
                        resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                        resultColor = Color.Red;
                        resultNicknameColor = ChatColorPresets.Red;
                    }
                }
                else
                {
                    if (location == "")
                    {
                        if (UsersData.UserGetData<string>(data.UserUUID, "userPlace") != "")
                        {
                            var weather = Tools.Get_weather(UsersData.UserGetData<string>(data.UserUUID, "userLat"), UsersData.UserGetData<string>(data.UserUUID, "userLon"));
                            var result = weather.Result.current;
                            if (result.temperature != -400)
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "weatherSucceful", data.ChannelID)
                                    .Replace("%emote%", Tools.GetWeatherEmoji(result.temperature))
                                    .Replace("%name%", UsersData.UserGetData<string>(data.UserUUID, "userPlace"))
                                    .Replace("%temperature%", result.temperature.ToString())
                                    .Replace("%feelsLike%", result.feels_like.ToString())
                                    .Replace("%windSpeed%", result.wind.speed.ToString())
                                    .Replace("%summary%", Tools.GetWeatherSummary(data.User.Lang, result.summary.ToString(), data.ChannelID))
                                    .Replace("%pressure%", result.pressure.ToString())
                                    .Replace("%uvIndex%", result.uv_index.ToString())
                                    .Replace("%humidity%", result.humidity.ToString())
                                    .Replace("%visibility%", result.visibility.ToString())
                                    .Replace("%skyEmote%", Tools.GetWeatherSummaryEmoji(result.summary.ToString()));
                                resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsTitle", data.ChannelID);
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "placeCrack", data.ChannelID);
                                resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                                resultColor = Color.Red;
                                resultNicknameColor = ChatColorPresets.Red;
                            }
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "emptyLoc", data.ChannelID);
                            resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;
                        }
                    }
                    else
                    {
                        try
                        {
                            var locations = Tools.Get_location(location);
                            locations.Wait();
                            var resultLoc = locations.Result;
                            if (resultLoc.Count == 0)
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "thatPlaceDoesntFound", data.ChannelID);
                                resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                                resultColor = Color.Red;
                                resultNicknameColor = ChatColorPresets.Red;
                            }
                            else if (resultLoc.Count == 1)
                            {
                                if (resultLoc.ElementAt(0).name == "err")
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "placeCrack", data.ChannelID);
                                    resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                                    resultColor = Color.Red;
                                    resultNicknameColor = ChatColorPresets.Red;
                                }
                                else
                                {
                                    var weather = Tools.Get_weather(resultLoc[0].lat, resultLoc[0].lon);
                                    weather.Wait();
                                    var result = weather.Result.current;
                                    if (result.temperature != -400)
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "weatherSucceful", data.ChannelID)
                                            .Replace("%emote%", Tools.GetWeatherEmoji(result.temperature))
                                            .Replace("%name%", resultLoc[0].name)
                                            .Replace("%temperature%", result.temperature.ToString())
                                            .Replace("%feelsLike%", result.feels_like.ToString())
                                            .Replace("%windSpeed%", result.wind.speed.ToString())
                                            .Replace("%summary%", Tools.GetWeatherSummary(data.User.Lang, result.summary.ToString(), data.ChannelID))
                                            .Replace("%pressure%", result.pressure.ToString())
                                            .Replace("%uvIndex%", result.uv_index.ToString())
                                            .Replace("%humidity%", result.humidity.ToString())
                                            .Replace("%visibility%", result.visibility.ToString())
                                            .Replace("%skyEmote%", Tools.GetWeatherSummaryEmoji(result.summary.ToString()));
                                        resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsTitle", data.ChannelID);
                                    }
                                    else
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "placeCrack", data.ChannelID);
                                        resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                                        resultColor = Color.Red;
                                        resultNicknameColor = ChatColorPresets.Red;
                                    }
                                }
                            }
                            else
                            {
                                List<string> jsons = new();
                                foreach (var loc in resultLoc)
                                {
                                    jsons.Add($"name: \"{loc.name}\", lat: \"{loc.lat}\", lon: \"{loc.lon}\"");
                                }
                                UsersData.UserSaveData(data.UserUUID, "weatherResultLocations", jsons);
                                string locationPage = "";
                                int maxPage = (int)Math.Ceiling((double)(resultLoc.Count / 5));
                                if (resultLoc.Count > 5)
                                {
                                    int index = 1;
                                    for (int i = 0; i < 5; i++)
                                    {
                                        locationPage += $"{index}. {resultLoc[index - 1].name} (lat: {Tools.ShortenCoordinate(resultLoc[index - 1].lat)}, lon: {Tools.ShortenCoordinate(resultLoc[index - 1].lon)}), ";
                                        index++;
                                    }
                                    locationPage = locationPage.TrimEnd(',', ' ');
                                }
                                else
                                {
                                    int index = 0;
                                    foreach (var location2 in resultLoc)
                                    {
                                        index++;
                                        locationPage += $"{index}. {location2.name} (lat: {location2.lat}, lon: {location2.lon}), ";
                                    }
                                }
                                locationPage = (locationPage + "\n").Replace(", \n", "");
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlaces", data.ChannelID)
                                    .Replace("%places%", locationPage)
                                    .Replace("%page%", "1")
                                    .Replace("%pages%", maxPage.ToString());
                                resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "aLotOfPlacesDsTitle", data.ChannelID);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWorker.LogError(ex.Message, "weather");
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "placeCrack", data.ChannelID);
                            resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "weatherDsErrTitle", data.ChannelID);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;
                        }
                    }
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
                    IsEmbed = false,
                    Ephemeral = false,
                    Title = resultMessageTitle,
                    Color = resultColor,
                    NickNameColor = resultNicknameColor
                };
            }
        }
    }
}
