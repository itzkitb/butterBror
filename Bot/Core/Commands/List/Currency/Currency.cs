using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using Newtonsoft.Json;

namespace bb.Core.Commands.List.Currency
{
    public class Currency : CommandBase
    {
        public override string Name => "currency";
        public override string Author => "https://github.com/voxelll1";
        public override string Source => "Currency/Currency.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Конвертер валют." },
            { Language.EnUs, "Currency converter." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 5;
        public override string[] Aliases => ["currency", "curr", "convert", "конвертер", "валюта"];
        public override string Help => "<amount> <from_currency> [to] <to_currency>";
        public override DateTime CreationDate => DateTime.Parse("2025-06-04T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Discord, Platform.Twitch, Platform.Telegram];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                if (data.Arguments != null && data.Arguments.Count > 1)
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

                    HashSet<string> currencySet = [.. existingCurrencies];

                    string? initialCurrency = null;
                    string? wantedCurrency = null;
                    ulong currencyQuantity = 0;

                    bool hasTo = data.ArgumentsString.Contains("to:", StringComparison.OrdinalIgnoreCase);
                    bool hasFrom = data.ArgumentsString.Contains("from:", StringComparison.OrdinalIgnoreCase);

                    if (hasTo || hasFrom)
                    {
                        var currencyArgs = data.Arguments
                            .Where(arg => currencySet.Contains(arg.ToUpper().Replace("TO:", "").Replace("FROM:", "")))
                            .ToList();

                        wantedCurrency = hasTo ? MessageProcessor.GetArgument(data.Arguments, "to") : currencyArgs.Count >= 1 ? currencyArgs[0] : null;

                        if (wantedCurrency is not null)
                        {
                            initialCurrency = hasFrom ? MessageProcessor.GetArgument(data.Arguments, "from") : currencyArgs.Count >= 2 ? currencyArgs[1] : null;
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

                    if (wantedCurrency is not null && initialCurrency is not null)
                    {
                        wantedCurrency = wantedCurrency.ToUpper();
                        initialCurrency = initialCurrency.ToUpper();

                        try
                        {
                            currencyQuantity = DataConversion.ToUlong(data.Arguments[0]);
                        }
                        catch
                        {
                            currencyQuantity = 1;
                        }

                        if (!currencySet.Contains(initialCurrency) || !currencySet.Contains(wantedCurrency))
                        {
                            string notFounded = !currencySet.Contains(initialCurrency) ? initialCurrency : wantedCurrency;
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:currency_not_found", data.ChannelId, data.Platform, notFounded));
                            return commandReturn;
                        }

                        var uri = new Uri($"https://open.er-api.com/v6/latest/{initialCurrency}");

                        using var client = new HttpClient();
                        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                        using var resp = await client.SendAsync(req);

                        CurrencyClass? res = JsonConvert.DeserializeObject<CurrencyClass>(await resp.Content.ReadAsStringAsync());

                        if (res == null || res.rates == null)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:API_error", string.Empty, data.Platform));
                            return commandReturn;
                        }

                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:currency",
                            data.ChannelId,
                            data.Platform,
                            currencyQuantity,
                            initialCurrency,
                            Math.Round(Convert.ToDouble(res.rates[wantedCurrency]) * currencyQuantity, 2),
                            wantedCurrency));
                    }
                    else
                    {
                        string? notFounded = !currencySet.Contains(initialCurrency ?? string.Empty) ? initialCurrency : wantedCurrency;
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:currency_not_found", data.ChannelId, data.Platform, notFounded ?? string.Empty));
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", data.ChannelId, data.Platform, $"{Program.BotInstance.DefaultCommandPrefix}currency 1 USD to RUB"));
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
            public string? result { get; set; }
            public string? provider { get; set; }
            public string? documentation { get; set; }
            public string? terms_of_use { get; set; }
            public double time_last_update_unix { get; set; }
            public string? time_last_update_utc { get; set; }
            public double time_next_update_unix { get; set; }
            public string? time_next_update_utc { get; set; }
            public double time_eol_unix { get; set; }
            public string? base_code { get; set; }
            public Dictionary<string, double>? rates { get; set; }
        }
    }
}