//using TwitchLib.Client.Events;
//using static butterBror.BotWorker;
//using static butterBror.BotWorker.FileMng;
//using Discord.WebSocket;

//namespace butterBror
//{
//    public partial class Commands
//    {
//        // #CMD 0A
//        public static void OnTwitchCommand(object sender, OnChatCommandReceivedArgs args)
//        {
//            SocketSlashCommand emp = default;
//            OnCommand("tw", args, emp);
//        }
//        public static void OnDiscordCommand(SocketSlashCommand dsCmd)
//        {
//            OnChatCommandReceivedArgs emp = default;
//            OnCommand("ds", emp, dsCmd);
//        }
//        public static void OnCommand(string platform, OnChatCommandReceivedArgs args, SocketSlashCommand dsCmd)
//        {
//            var allowed = true;
//            if (platform == "tw")
//            {
//                allowed = !args.Command.ChatMessage.IsMe;
//            }
//            if (allowed)
//            {
//                var lang = "ru";
//                try
//                {
//                    if (platform == "tw")
//                    {
//                        if (UsersData.UserGetData<string>(args.Command.ChatMessage.UserId, "language") == default)
//                        {
//                            UsersData.UserSaveData(args.Command.ChatMessage.UserId, "language", "ru");
//                        }
//                        else
//                        {
//                            lang = UsersData.UserGetData<string>(args.Command.ChatMessage.UserId, "language");
//                        }
//                    }
//                    else if (platform == "ds")
//                    {
//                        if (UsersData.UserGetData<string>("ds" + dsCmd.User.Id.ToString(), "language") == default)
//                        {
//                            UsersData.UserSaveData("ds" + dsCmd.User.Id.ToString(), "language", "ru");
//                        }
//                        else
//                        {
//                            lang = UsersData.UserGetData<string>("ds" + dsCmd.User.Id.ToString(), "language");
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Tools.ErrorOccured(ex.Message, "cmd0A");
//                }
//                try
//                {
//                    Bot.CommandsActive = 0;
//                    string[] pingAliases = ["ping", "пинг", "понг", "пенг", "п"]; add();
//                    string[] statusAliases = ["status", "stat", "статус", "стат"]; add();
//                    string[] winterAliases = ["winter", "w", "зима"]; add();
//                    string[] springAliases = ["spring", "sp", "весна"]; add();
//                    string[] summerAliases = ["summer", "su", "лето"]; add();
//                    string[] autumnAliases = ["autumn", "a", "осень"]; add();
//                    string[] firstGlobalLine = ["fgl", "firstgloballine", "пргс", "первоеглобальноесообщение"]; add();
//                    string[] lastLine = ["ll", "lastline", "пс", "последнеесообщение"]; add();
//                    string[] lastGlobalLine = ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"]; add();
//                    string[] firstLine = ["fl", "firstline", "прс", "первоесообщение"]; add();
//                    string[] bot = ["bot", "bt", "бот", "бт"]; add();
//                    string[] me = ["me", "m", "я"]; add();
//                    string[] restart = ["restart", "reload", "перезагрузить", "рестарт"]; add();
//                    string[] miningVideocard = ["mining", "mng", "манинг", "майн", "мнг"]; add();
//                    string[] calc = ["calc", "calculate", "кальк", "math", "матем", "математика", "калькулятор"]; add();
//                    string[] weather = ["weather", "погода", "wthr", "пгд"]; add();
//                    string[] location = ["location", "loc", "локация", "лок"]; add();
//                    string[] js = ["js", "javascript", "джава", "jaba", "supinic"]; add();
//                    string[] help = ["help", "sos", "info", "помощь", "информация", "info", "инфо"]; add();
//                    string[] gptCmd = ["gpt", "гпт"]; add();
//                    string[] tuckCmd = ["tuck", "уложить", "tk", "улож", "тык"]; add();
//                    string[] vhsTapes = ["cassette", "vhs", "foundfootage", "footage"]; add();
//                    string[] pizzas = ["pizza", "хуица", "пицца"]; add();
//                    string[] channelEmotes = ["emote", "emotes", "эмоут", "эмоуты"]; add();
//                    string[] balanceLol = ["balance", "баланс", "bal", "бал", "кошелек", "wallet"]; add();
//                    string[] fishingAliases = ["fishing", "fish", "рыба", "рыбалка"]; add();
//                    string[] imgurUploadImage = ["ui", "imgur", "upload", "uploadimage", "загрузитькартинку", "зк", "imguruploadimage"]; add();

//                    // AFK
//                    string[] draw = ["draw", "drw", "d", "рисовать", "рис", "р"]; add();
//                    string[] afk = ["afk", "афк"]; add();
//                    string[] sleep = ["sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с"]; add();
//                    string[] rest = ["rest", "nap", "r", "отдых", "отдохнуть", "о"]; add();
//                    string[] lurk = ["lurk", "l", "наблюдатьизтени", "спрятаться"]; add();
//                    string[] study = ["study", "st", "учеба", "учится", "у"]; add();
//                    string[] poop = ["poop", "p", "😳", "туалет", "🚽"]; add();
//                    string[] shower = ["shower", "sh", "ванная", "душ"]; add();
//                    string[] rafk = ["rafk", "рафк", "вафк", "вернутьафк", "resumeafk"]; add();

//                    var command = "";

//                    if (platform == "tw")
//                    {
//                        command = Tools.RemoveNonLatters(args.Command.CommandText.ToLower()).Replace("ё", "е");
//                    }
//                    else if (platform == "ds")
//                    {
//                        command = Tools.RemoveNonLatters(dsCmd.CommandName.ToLower()).Replace("ё", "е");
//                    }

//                    var userID = "";

//                    if (platform == "tw")
//                    {
//                        userID = args.Command.ChatMessage.UserId;
//                    }
//                    else if (platform == "ds")
//                    {
//                        userID = "ds" + dsCmd.User.Id;
//                    }

//                    if (!UsersData.UserGetData<bool>(userID, "isBanned"))
//                    {
//                        if (!UsersData.UserGetData<bool>(userID, "isIgnored"))
//                        {
//                            if (pingAliases.Contains(command))
//                            {
//                                pinger(args, dsCmd, lang, platform);
//                            }
//                            else if (statusAliases.Contains(command))
//                            {
//                                Status(args, dsCmd, lang, platform);
//                            }
//                            else if (winterAliases.Contains(command))
//                            {
//                                Winter(args, lang);
//                            }
//                            else if (springAliases.Contains(command))
//                            {
//                                Spring(args, lang);
//                            }
//                            else if (autumnAliases.Contains(command))
//                            {
//                                Autumn(args, lang);
//                            }
//                            else if (summerAliases.Contains(command))
//                            {
//                                Summer(args, lang);
//                            }
//                            else if (firstGlobalLine.Contains(command))
//                            {
//                                FirstGlobalLine(args, lang);
//                            }
//                            else if (lastLine.Contains(command))
//                            {
//                                LastLine(args, lang);
//                            }
//                            else if (firstLine.Contains(command))
//                            {
//                                FirstLine(args, lang);
//                            }
//                            else if (lastGlobalLine.Contains(command))
//                            {
//                                LastGlobalLine(args, lang);
//                            }
//                            else if (me.Contains(command))
//                            {
//                                Me(args, lang);
//                            }
//                            else if (restart.Contains(command))
//                            {
//                                Restart(args, lang);
//                            }
//                            else if (miningVideocard.Contains(command))
//                            {

//                            }
//                            else if (bot.Contains(command))
//                            {
//                                BotCommand(args, lang);
//                            }
//                            else if (calc.Contains(command))
//                            {
//                                calculator(args, lang);
//                            }
//                            else if (weather.Contains(command))
//                            {
//                                Weather(args, dsCmd, lang, platform);
//                            }
//                            else if (location.Contains(command))
//                            {
//                                Tools.executedCommand(args, "location");
//                                if (Tools.IsNotOnCooldown(30, 10, "Location", userID, args.Command.ChatMessage.RoomId))
//                                {
//                                    Tools.SendMsgReply(args.Command.ChatMessage.Channel, args.Command.ChatMessage.RoomId, "🔧 В переделывании...", args.Command.ChatMessage.Id, lang);
//                                }
//                            }
//                            else if (js.Contains(command))
//                            {
//                                java(args, lang);
//                            }
//                            else if (afk.Contains(command))
//                            {
//                                Afk(args, lang, "afk");
//                            }
//                            else if (sleep.Contains(command))
//                            {
//                                Afk(args, lang, "sleep");
//                            }
//                            else if (lurk.Contains(command))
//                            {
//                                Afk(args, lang, "lurk");
//                            }
//                            else if (rest.Contains(command))
//                            {
//                                Afk(args, lang, "rest");
//                            }
//                            else if (study.Contains(command))
//                            {
//                                Afk(args, lang, "study");
//                            }
//                            else if (draw.Contains(command))
//                            {
//                                Afk(args, lang, "draw");
//                            }
//                            else if (poop.Contains(command))
//                            {
//                                Afk(args, lang, "poop");
//                            }
//                            else if (shower.Contains(command))
//                            {
//                                Afk(args, lang, "shower");
//                            }
//                            else if (help.Contains(command))
//                            {
//                                Tools.executedCommand(args, "help");
//                                if (Tools.IsNotOnCooldown(300, 30, "Help", userID, args.Command.ChatMessage.RoomId))
//                                {
//                                    Tools.SendMsgReply(args.Command.ChatMessage.Channel, args.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "help"), args.Command.ChatMessage.Id, lang, true);
//                                }
//                            }
//                            else if (rafk.Contains(command))
//                            {
//                                Rafk(args, lang);
//                            }
//                            else if (gptCmd.Contains(command))
//                            {
//                                GPT(args, lang);
//                            }
//                            else if (tuckCmd.Contains(command))
//                            {
//                                Tuck(args, lang);
//                            }
//                            else if (vhsTapes.Contains(command))
//                            {
//                                vhsCommand(args, lang);
//                            }
//                            else if (command == "test")
//                            {
//                                if (UsersData.UserGetData<bool>(args.Command.ChatMessage.UserId, "isBotDev"))
//                                {
//                                    Tools.SendMsgReply(args.Command.ChatMessage.Channel, args.Command.ChatMessage.RoomId, "Длинные тексты (лонгриды), где большой объем сочетается с глубоким погружением в тему, становятся все более популярными в печатных и онлайновых изданиях, так как позволяют изданию выделиться из информационного шума. Цели исследования – выявить распространенность лонгридов в российских СМИ и содержательные и композиционные особенности этих текстов. Исследование включает мониторинг публикаций в центральных российских изданиях и последующий контент-анализ 10 материалов из 10 печатных и онлайновых изданий. Выводы исследования: лонгриды присутствуют в изданиях разных типов: от ежедневных газет − до нишевых новостных сайтов. Они посвящены, как правило, описанию нового явления; имеют объем от 2 до 4 тыс. слов и построены по композиционной схеме чередования примеров и обобщений.", args.Command.ChatMessage.Id, lang, true);
//                                }
//                            }
//                            else if (pizzas.Contains(command))
//                            {
//                                pizza(args, lang);
//                            }
//                            else if (channelEmotes.Contains(command))
//                            {
//                                emotes(args, lang);
//                            }
//                            else if (balanceLol.Contains(command))
//                            {
//                                balance(args, lang);
//                            }
//                            else if (fishingAliases.Contains(command))
//                            {
//                                fishing(args, lang);
//                            }
//                            else if (imgurUploadImage.Contains(command))
//                            {
//                                UploadToImgur(args, dsCmd, lang, platform);
//                            }
//                        }
//                        else
//                        {
//                            if (bot.Contains(command))
//                            {
//                                BotCommand(args, lang);
//                            }
//                            else
//                            {
//                                LogWorker.LogInfo($"{args.Command.ChatMessage.Username} попробовал выполнить команду, но был заигнорен (#{args.Command.CommandText} {args.Command.ArgumentsAsString})", "CMD");
//                            }
//                        }
//                    }
//                    else
//                    {
//                        LogWorker.LogInfo($"{args.Command.ChatMessage.Username} попробовал выполнить команду, но он был забанен (#{args.Command.CommandText} {args.Command.ArgumentsAsString})", "CMD");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Tools.ErrorOccured(ex.Message, "CMD");
//                    if (platform == "tw")
//                    {
//                        Tools.SendMsgReply(args.Command.ChatMessage.Channel, args.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "error"), args.Command.ChatMessage.Id, lang, true);
//                    }
//                }
//            }
//        }
//        public static void add()
//        {
//            Bot.CommandsActive++;
//        }
//    }
//}
