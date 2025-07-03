using butterBror.Utils;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;
using Microsoft.TeamFoundation.Common;
using Microsoft.VisualStudio.Services.Common.CommandLine;
using Newtonsoft.Json;

namespace butterBror
{
    public partial class Commands
    {
        public class Currency
        {
            public static CommandInfo Info = new()
            {
                Name = "currency",
                Author = "@voxelll",
                AuthorLink = "twitch.tv/voxelll_",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/b16a7016-6f41-429c-add7-c1b92d514027-profile_image-300x300.jpeg",
                Description = new() {
                    { "ru", "Конвертер валют" },
                    { "en", "Currency converter" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=currency",
                CooldownPerUser = 10,
                CooldownPerChannel = 5,
                Aliases = ["currency", "curr", "convert", "конвертер", "валюта"],
                Arguments = "<currency quantity> [initialCurrency] to [wantedCurrency]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("04/06/2025"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Discord, Platforms.Twitch, Platforms.Telegram]
            };

            public async Task<CommandReturn> Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.Arguments.Count > 1)
                    {
                        string[] existingCurrencies = new string[]
                        {
                            "USD", "AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AUD", "AWG",
                            "AZN", "BAM", "BBD", "BDT", "BGN", "BHD", "BIF", "BMD", "BND", "BOB",
                            "BRL", "BSD", "BTN", "BWP", "BYN", "BZD", "CAD", "CDF", "CHF", "CLP",
                            "CNY", "COP", "CRC", "CUP", "CVE", "CZK", "DJF", "DKK", "DOP", "DZD",
                            "EGP", "ERN", "ETB", "EUR", "FJD", "FKP", "FOK", "GBP", "GEL", "GGP",
                            "GHS", "GIP", "GMD", "GNF", "GTQ", "GYD", "HKD", "HNL", "HRK", "HTG",
                            "HUF", "IDR", "ILS", "IMP", "INR", "IQD", "IRR", "ISK", "JEP", "JMD",
                            "JOD", "JPY", "KES", "KGS", "KHR", "KID", "KMF", "KRW", "KWD", "KYD",
                            "KZT", "LAK", "LBP", "LKR", "LRD", "LSL", "LYD", "MAD", "MDL", "MGA",
                            "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR", "MWK", "MXN", "MYR",
                            "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "OMR", "PEN", "PGK",
                            "PHP", "PKR", "PLN", "PYG", "QAR", "RON", "RSD", "RUB", "RWF", "SAR",
                            "SBD", "SCR", "SDG", "SEK", "SGD", "SHP", "SLE", "SLL", "SOS", "SRD",
                            "SSP", "STN", "SYP", "SZL", "THB", "TJS", "TMT", "TND", "TOP", "TRY",
                            "TTD", "TVD", "TWD", "TZS", "UAH", "UGX", "UYU", "UZS", "VES", "VND",
                            "VUV", "WST", "XAF", "XCD", "XCG", "XDR", "XOF", "XPF", "YER", "ZAR",
                            "ZMW", "ZWL"
                        };

                        HashSet<string> currencySet = new HashSet<string>(existingCurrencies);

                        string initialCurrency = null;
                        string wantedCurrency = null;
                        ulong currencyQuantity = 0;

                        bool hasTo = data.ArgumentsString.Contains("to:", StringComparison.OrdinalIgnoreCase);
                        bool hasFrom = data.ArgumentsString.Contains("from:", StringComparison.OrdinalIgnoreCase);

                        if (hasTo || hasFrom)
                        {
                            var currencyArgs = data.Arguments
                                .Where(arg => currencySet.Contains(arg.ToUpper().Replace("TO:", "").Replace("FROM:", "")))
                                .ToList();

                            wantedCurrency = hasTo ? Command.GetArgument(data.Arguments, "to") : currencyArgs.Count >= 1 ? currencyArgs[0] : null;

                            if (!wantedCurrency.IsNullOrEmpty())
                            {
                                initialCurrency = hasFrom ? Command.GetArgument(data.Arguments, "from") : currencyArgs.Count >= 2 ? currencyArgs[1] : null;
                            }
                        }
                        else
                        {
                            var currencyArgs = data.Arguments
                                .Where(arg => currencySet.Contains(arg.ToUpper()))
                                .ToList();

                            if (currencyArgs.Count >= 1) initialCurrency = currencyArgs[0];
                            if (currencyArgs.Count >= 2) wantedCurrency = currencyArgs[1];
                        }

                        if (!wantedCurrency.IsNullOrEmpty() && !initialCurrency.IsNullOrEmpty())
                        {
                            wantedCurrency = wantedCurrency.ToUpper();
                            initialCurrency = initialCurrency.ToUpper();

                            try
                            {
                                currencyQuantity = Format.ToUlong(data.Arguments[0]);
                            }
                            catch 
                            {
                                currencyQuantity = 1;
                            }

                            if (!currencySet.Contains(initialCurrency) || !currencySet.Contains(wantedCurrency))
                            {
                                string notFounded = !currencySet.Contains(initialCurrency) ? initialCurrency : wantedCurrency;
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:currency_not_found", data.ChannelID, data.Platform, new()
                                {
                                    { "currency", notFounded }
                                }));
                                return commandReturn;
                            }

                            var uri = new Uri($"https://open.er-api.com/v6/latest/{initialCurrency}");

                            using var client = new HttpClient();
                            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                            using var resp = await client.SendAsync(req);

                            CurrencyClass res = JsonConvert.DeserializeObject<CurrencyClass>(await resp.Content.ReadAsStringAsync());

                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:currency", data.ChannelID, data.Platform, new Dictionary<string, string>()
                            {
                                { "currencyQuantity", currencyQuantity.ToString() },
                                { "initialCurrency", initialCurrency.ToString() },
                                { "result", Math.Round(Convert.ToDouble(res.rates[wantedCurrency]) * currencyQuantity, 2).ToString() },
                                { "wantedCurrency", wantedCurrency }
                            }));
                        }
                        else
                        {
                            string notFounded = !currencySet.Contains(initialCurrency) ? initialCurrency : wantedCurrency;
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:currency_not_found", data.ChannelID, data.Platform, new()
                                {
                                    { "currency", notFounded }
                                }));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform)
                            .Replace("%command_example%", $"{Core.Bot.Executor}currency 1 USD to RUB"));
                    }
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }

            public class CurrencyClass
            {
                public string result { get; set; }
                public string provider { get; set; }
                public string documentation { get; set; }
                public string terms_of_use { get; set; }
                public double time_last_update_unix { get; set; }
                public string time_last_update_utc { get; set; }
                public double time_next_update_unix { get; set; }
                public string time_next_update_utc { get; set; }
                public double time_eol_unix { get; set; }
                public string base_code { get; set; }
                public Dictionary<string, double> rates { get; set; }
            }
        }
    }
}
