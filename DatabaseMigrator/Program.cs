using System;
using System.IO;
using System.Data.SQLite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class DatabaseMigrator
{
    static void Main()
    {
        string usersDbPath = "Users.db";
        string channelsDbPath = "Channels.db";
        string messagesDbPath = "Messages.db";
        string gamesDbPath = "Games.db";

        string userDbRoot = "USERSDB";
        string channelsRoot = "CHNLS";
        string convrtRoot = "CONVRT";
        string gamesRoot = "GAMES_DATA";

        Console.WriteLine("=== НАЧАЛО МИГРАЦИИ ДАННЫХ ===");
        Console.WriteLine($"Пользовательские данные: {usersDbPath}");
        Console.WriteLine($"Данные каналов: {channelsDbPath}");
        Console.WriteLine($"Сообщения пользователей: {messagesDbPath}");
        Console.WriteLine($"Игровые данные: {gamesDbPath}");

        // Создаем подключения к всем базам данных
        using (var usersConnection = new SQLiteConnection($"Data Source={usersDbPath};Version=3;"))
        using (var channelsConnection = new SQLiteConnection($"Data Source={channelsDbPath};Version=3;"))
        using (var messagesConnection = new SQLiteConnection($"Data Source={messagesDbPath};Version=3;"))
        using (var gamesConnection = new SQLiteConnection($"Data Source={gamesDbPath};Version=3;"))
        {
            // Инициализируем все соединения
            usersConnection.Open();
            channelsConnection.Open();
            messagesConnection.Open();
            gamesConnection.Open();

            Console.WriteLine("\nПодключено ко всем базам данных. Начинаем миграцию...");

            // Миграция пользовательских данных
            MigrateUsers(usersConnection, userDbRoot, convrtRoot);

            // Миграция данных каналов
            if (Directory.Exists(channelsRoot))
            {
                Console.WriteLine("\n" + new string('-', 50));
                Console.WriteLine("МИГРАЦИЯ ДАННЫХ КАНАЛОВ");
                Console.WriteLine(new string('-', 50));
                MigrateChannels(channelsConnection, messagesConnection, channelsRoot);
            }
            else
            {
                Console.WriteLine("\nПапка CHNLS не найдена. Пропускаем миграцию каналов.");
            }

            MigrateChannelMessagesCount(usersConnection, channelsRoot);

            // Миграция игровых данных
            if (Directory.Exists(gamesRoot))
            {
                Console.WriteLine("\n" + new string('-', 50));
                Console.WriteLine("МИГРАЦИЯ ИГРОВЫХ ДАННЫХ");
                Console.WriteLine(new string('-', 50));
                MigrateGames(gamesConnection, gamesRoot);
            }
            else
            {
                Console.WriteLine("\nПапка GAMES_DATA не найдена. Пропускаем миграцию игровых данных.");
            }

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("МИГРАЦИЯ ЗАВЕРШЕНА УСПЕШНО!");
            Console.WriteLine(new string('=', 50));
            Console.ReadLine();
        }
    }

    // ========== Миграция пользовательских данных ==========
    static void MigrateUsers(SQLiteConnection connection, string userDbRoot, string convrtRoot)
    {
        if (!Directory.Exists(userDbRoot))
        {
            Console.WriteLine($"Ошибка: Папка {userDbRoot} не найдена.");
            return;
        }

        Console.WriteLine("\n=== МИГРАЦИЯ ПОЛЬЗОВАТЕЛЬСКИХ ДАННЫХ ===");
        Console.WriteLine($"Источник: {userDbRoot}");

        // Создаем таблицу для маппинга ID-никнейм
        CreateUsernameMappingTable(connection);

        foreach (var platformPath in Directory.GetDirectories(userDbRoot))
        {
            string platformName = new DirectoryInfo(platformPath).Name;
            Console.WriteLine($"\nОбработка платформы пользователей: {platformName}");

            // Создаем таблицу для платформы
            CreatePlatformTable(connection, platformName);

            // Обрабатываем все JSON-файлы в папке платформы
            ProcessPlatformFiles(connection, platformPath, platformName);
        }

        // Мигрируем данные CONVRT
        if (Directory.Exists(convrtRoot))
        {
            Console.WriteLine("\n" + new string('-', 40));
            Console.WriteLine("МИГРАЦИЯ ДАННЫХ CONVRT");
            Console.WriteLine(new string('-', 40));
            MigrateConvrtData(connection, convrtRoot);
        }
        else
        {
            Console.WriteLine("\nПапка CONVRT не найдена. Пропускаем миграцию маппинга ID-никнейм.");
        }
    }

    static void CreateUsernameMappingTable(SQLiteConnection connection)
    {
        string createTableSql = @"
            CREATE TABLE IF NOT EXISTS UsernameMapping (
                Platform TEXT NOT NULL,
                UserID INTEGER NOT NULL,
                Username TEXT NOT NULL,
                PRIMARY KEY (Platform, UserID, Username)
            );";

        using (var cmd = new SQLiteCommand(createTableSql, connection))
        {
            cmd.ExecuteNonQuery();
        }
    }

    static void CreatePlatformTable(SQLiteConnection connection, string platformName)
    {
        // Убираем недопустимые символы из названия таблицы
        string safeTableName = SanitizeTableName(platformName);

        string createTableSql = $@"
            CREATE TABLE ""{safeTableName}"" (
	""ID""	INTEGER,
	""FirstMessage""	TEXT DEFAULT '',
	""FirstSeen""	TEXT DEFAULT '2000-01-01T00:00:00.0000000+00:00',
	""FirstChannel""	TEXT DEFAULT '',
	""LastMessage""	TEXT DEFAULT '',
	""LastSeen""	TEXT DEFAULT '2000-01-01T00:00:00.0000000+00:00',
	""LastChannel""	TEXT DEFAULT '',
	""Balance""	INTEGER DEFAULT 0,
	""AfterDotBalance""	INTEGER DEFAULT 0,
	""Rating""	INTEGER DEFAULT 500,
	""IsAFK""	INTEGER DEFAULT 0,
	""AFKText""	TEXT DEFAULT '',
	""AFKType""	TEXT DEFAULT '',
	""Reminders""	TEXT DEFAULT '{{}}',
	""LastCookie""	TEXT DEFAULT '2000-01-01T00:00:00.0000000+00:00',
	""GiftedCookies""	INTEGER DEFAULT 0,
	""EatedCookies""	INTEGER DEFAULT 0,
	""BuyedCookies""	INTEGER DEFAULT 0,
	""ReceivedCookies""	INTEGER DEFAULT 0,
	""Location""	TEXT DEFAULT '',
	""Longitude""	TEXT DEFAULT '',
	""Latitude""	TEXT DEFAULT '',
	""Language""	TEXT DEFAULT 'en-US',
	""AFKStart""	TEXT DEFAULT '2000-01-01T00:00:00.0000000+00:00',
	""AFKResume""	TEXT DEFAULT '2000-01-01T00:00:00.0000000+00:00',
	""AFKResumeTimes""	INTEGER DEFAULT 0,
	""LastUse""	TEXT DEFAULT '{{}}',
	""GPTHistory""	TEXT DEFAULT '{{}}',
	""WeatherResultLocations""	TEXT DEFAULT '{{}}',
	""TotalMessages""	INTEGER DEFAULT 0,
	""TotalMessagesLength""	INTEGER DEFAULT 0,
	""ChannelMessagesCount""	TEXT DEFAULT '{{}}',
	PRIMARY KEY(""ID"")
)";

        using (var cmd = new SQLiteCommand(createTableSql, connection))
        {
            cmd.ExecuteNonQuery();
        }
    }

    static void ProcessPlatformFiles(SQLiteConnection connection, string platformPath, string platformName)
    {
        int processedFiles = 0;
        int errorCount = 0;
        string safeTableName = SanitizeTableName(platformName);

        using (var transaction = connection.BeginTransaction())
        {
            foreach (var jsonFile in Directory.GetFiles(platformPath, "*.json"))
            {
                try
                {
                    ProcessJsonFile(connection, transaction, jsonFile, safeTableName);
                    processedFiles++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    if (errorCount <= 3) // Показываем только первые 3 ошибки
                        Console.WriteLine($"  ! Ошибка при обработке {Path.GetFileName(jsonFile)}: {ex.Message}");
                }
            }
            transaction.Commit();
        }

        Console.WriteLine($"  Обработано файлов: {processedFiles} | Ошибок: {errorCount}");
    }

    static void ProcessJsonFile(SQLiteConnection connection, SQLiteTransaction transaction,
                               string jsonFile, string tableName)
    {
        string userIdStr = Path.GetFileNameWithoutExtension(jsonFile);
        long userId = long.Parse(userIdStr);
        string jsonContent = File.ReadAllText(jsonFile);
        JObject jsonData = JObject.Parse(jsonContent);

        string insertSql = $@"
            INSERT OR REPLACE INTO [{tableName}] (
                ID, FirstMessage, FirstSeen, FirstChannel, LastMessage, LastSeen, LastChannel,
                Balance, AfterDotBalance, Rating, IsAFK, AFKText, AFKType, Reminders, LastCookie,
                GiftedCookies, EatedCookies, BuyedCookies, Location, Longitude, Latitude, Language,
                AFKStart, AFKResume, AFKResumeTimes, LastUse, GPTHistory, WeatherResultLocations,
                TotalMessages, TotalMessagesLength
            ) VALUES (
                @ID, @FirstMessage, @FirstSeen, '', @LastMessage, @LastSeen, @LastChannel,
                @Balance, @AfterDotBalance, @Rating, @IsAFK, @AFKText, @AFKType, '[]', @LastCookie,
                @GiftedCookies, @EatedCookies, @BuyedCookies, @Location, @Longitude, @Latitude, @Language,
                @AFKStart, @AFKResume, @AFKResumeTimes, '[]', '[]', '[]',
                @TotalMessages, 0
            );";

        using (var cmd = new SQLiteCommand(insertSql, connection, transaction))
        {
            // Основные параметры
            cmd.Parameters.AddWithValue("@ID", userId);
            cmd.Parameters.AddWithValue("@FirstMessage", GetNullableString(jsonData["firstMessage"]));
            cmd.Parameters.AddWithValue("@FirstSeen", GetNullableString(jsonData["firstSeen"]));
            cmd.Parameters.AddWithValue("@LastMessage", GetNullableString(jsonData["lastSeenMessage"]));
            cmd.Parameters.AddWithValue("@LastSeen", GetNullableString(jsonData["lastSeen"]));
            cmd.Parameters.AddWithValue("@LastChannel", GetNullableString(jsonData["lastSeenChannel"]));

            // Числовые параметры
            cmd.Parameters.AddWithValue("@Balance", GetNullableInt(jsonData["balance"]));
            cmd.Parameters.AddWithValue("@AfterDotBalance", GetNullableInt(jsonData["floatBalance"]));
            cmd.Parameters.AddWithValue("@Rating", GetNullableInt(jsonData["rating"]));
            cmd.Parameters.AddWithValue("@IsAFK", GetIsAfkValue(jsonData["isAfk"]));
            cmd.Parameters.AddWithValue("@GiftedCookies", GetNullableInt(jsonData["giftedCookies"]));
            cmd.Parameters.AddWithValue("@EatedCookies", GetNullableInt(jsonData["eatedCookies"]));
            cmd.Parameters.AddWithValue("@BuyedCookies", GetNullableInt(jsonData["buyedCookies"]));
            cmd.Parameters.AddWithValue("@AFKResumeTimes", GetNullableInt(jsonData["fromAfkResumeTimes"]));
            cmd.Parameters.AddWithValue("@TotalMessages", GetNullableInt(jsonData["totalMessages"]));

            // Специальные обработки
            cmd.Parameters.AddWithValue("@AFKText", GetNullableString(jsonData["afkText"]));
            cmd.Parameters.AddWithValue("@AFKType", GetNullableString(jsonData["afkType"]));
            cmd.Parameters.AddWithValue("@LastCookie", GetNullableString(jsonData["lastCookieEat"]));
            cmd.Parameters.AddWithValue("@Location", GetNullableString(jsonData["userPlace"]));
            cmd.Parameters.AddWithValue("@Longitude", GetNullableString(jsonData["userLon"]));
            cmd.Parameters.AddWithValue("@Latitude", GetNullableString(jsonData["userLat"]));
            cmd.Parameters.AddWithValue("@Language", ProcessLanguage(jsonData["language"]?.ToString()));
            cmd.Parameters.AddWithValue("@AFKStart", GetNullableString(jsonData["afkTime"]));
            cmd.Parameters.AddWithValue("@AFKResume", GetNullableString(jsonData["lastFromAfkResume"]));

            cmd.ExecuteNonQuery();
        }
    }

    // ========== Миграция данных CONVRT ==========
    static void MigrateConvrtData(SQLiteConnection connection, string convrtRoot)
    {
        int totalMappings = 0;
        int errors = 0;

        // Обрабатываем I2N (ID to Name)
        string i2nRoot = Path.Combine(convrtRoot, "I2N");
        if (Directory.Exists(i2nRoot))
        {
            Console.WriteLine("Обработка I2N (ID → никнейм):");

            foreach (var platformPath in Directory.GetDirectories(i2nRoot))
            {
                string platformName = new DirectoryInfo(platformPath).Name;
                Console.WriteLine($"  Платформа: {platformName}");

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var file in Directory.GetFiles(platformPath, "*.txt"))
                    {
                        try
                        {
                            string userIdStr = Path.GetFileNameWithoutExtension(file);
                            long userId = long.Parse(userIdStr);
                            string username = File.ReadAllText(file).Trim();

                            if (!string.IsNullOrEmpty(username))
                            {
                                InsertUsernameMapping(connection, transaction, platformName, userId, username);
                                totalMappings++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            if (errors <= 3)
                                Console.WriteLine($"    ! Ошибка при обработке {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        // Обрабатываем N2I (Name to ID)
        string n2iRoot = Path.Combine(convrtRoot, "N2I");
        if (Directory.Exists(n2iRoot))
        {
            Console.WriteLine("\nОбработка N2I (никнейм → ID):");

            foreach (var platformPath in Directory.GetDirectories(n2iRoot))
            {
                string platformName = new DirectoryInfo(platformPath).Name;
                Console.WriteLine($"  Платформа: {platformName}");

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var file in Directory.GetFiles(platformPath, "*.txt"))
                    {
                        try
                        {
                            string username = Path.GetFileNameWithoutExtension(file);
                            string userIdStr = File.ReadAllText(file).Trim();

                            if (long.TryParse(userIdStr, out long userId))
                            {
                                InsertUsernameMapping(connection, transaction, platformName, userId, username);
                                totalMappings++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            if (errors <= 3)
                                Console.WriteLine($"    ! Ошибка при обработке {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        Console.WriteLine($"\nИтоги миграции CONVRT:");
        Console.WriteLine($"  Создано маппингов: {totalMappings} | Ошибок: {errors}");
    }

    static void InsertUsernameMapping(SQLiteConnection connection, SQLiteTransaction transaction,
        string platform, long userId, string username)
    {
        string insertSql = @"
            INSERT OR IGNORE INTO UsernameMapping (Platform, UserID, Username)
            VALUES (@Platform, @UserID, @Username);";

        using (var cmd = new SQLiteCommand(insertSql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("@Platform", platform);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.ExecuteNonQuery();
        }
    }

    // ========== НОВАЯ ЧАСТЬ: Миграция данных каналов ==========
    static void MigrateChannels(SQLiteConnection channelsConnection,
                               SQLiteConnection messagesConnection,
                               string channelsRoot)
    {
        Console.WriteLine($"Источник: {channelsRoot}");
        Console.WriteLine("Структура баз: Channels.db (каналы), Messages.db (сообщения)");

        int totalChannels = 0;
        int totalErrors = 0;

        foreach (var platformPath in Directory.GetDirectories(channelsRoot))
        {
            string platformName = new DirectoryInfo(platformPath).Name;
            string safePlatform = SanitizeTableName(platformName);

            Console.WriteLine($"\n{new string('-', 40)}");
            Console.WriteLine($"ОБРАБОТКА ПЛАТФОРМЫ: {platformName}");
            Console.WriteLine(new string('-', 40));

            // Создаем таблицы для этой платформы в Channels.db
            CreateChannelTablesForPlatform(channelsConnection, platformName);

            foreach (var channelPath in Directory.GetDirectories(platformPath))
            {
                string channelId = new DirectoryInfo(channelPath).Name;
                totalChannels++;

                Console.WriteLine($"  ➤ Канал: {channelId}");

                using (var channelsTransaction = channelsConnection.BeginTransaction())
                using (var messagesTransaction = messagesConnection.BeginTransaction())
                {
                    bool channelSuccess = true;
                    Exception channelException = null;

                    try
                    {
                        // 1. Обрабатываем основные данные канала (CDD и BANWORDS)
                        ProcessChannelCoreData(channelsConnection, channelsTransaction,
                            platformName, channelId, channelPath);

                        // 2. Обрабатываем первые сообщения пользователей
                        ProcessFirstMessages(channelsConnection, channelsTransaction,
                            platformName, channelId, Path.Combine(channelPath, "FM"));

                        // 3. Обрабатываем историю сообщений (в Messages.db)
                        ProcessUserMessages(messagesConnection, messagesTransaction,
                            platformName, channelId, Path.Combine(channelPath, "MSGS"));

                        channelsTransaction.Commit();
                        messagesTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        channelsTransaction.Rollback();
                        messagesTransaction.Rollback();
                        channelSuccess = false;
                        channelException = ex;
                        totalErrors++;
                    }

                    if (channelSuccess)
                        Console.WriteLine($"    ✓ Успешно обработан");
                    else
                        Console.WriteLine($"    ✗ ОШИБКА: {channelException.Message}");
                }
            }
        }

        Console.WriteLine($"\nИТОГИ МИГРАЦИИ КАНАЛОВ:");
        Console.WriteLine($"  Обработано каналов: {totalChannels}");
        Console.WriteLine($"  Ошибок: {totalErrors}");
    }

    static void CreateChannelTablesForPlatform(SQLiteConnection connection, string platform)
    {
        string safePlatform = SanitizeTableName(platform);

        // Таблица для основной информации о каналах
        string createChannelsTable = $@"
            CREATE TABLE IF NOT EXISTS ""{safePlatform}"" (
	            ""ChannelID""	TEXT,
	            ""CDDData""	TEXT DEFAULT '{{}}',
	            ""BanWords""	TEXT DEFAULT '[]',
	            PRIMARY KEY(""ChannelID"")
            )";

        // Таблица для первых сообщений пользователей в каналах
        string createFirstMessagesTable = $@"
            CREATE TABLE IF NOT EXISTS [FirstMessage_{safePlatform}] (
                ChannelID TEXT NOT NULL,
                UserID INTEGER NOT NULL,
                MessageDate TEXT,
                MessageText TEXT,
                IsMe INTEGER,
                IsModerator INTEGER,
                IsSubscriber INTEGER,
                IsPartner INTEGER,
                IsStaff INTEGER,
                IsTurbo INTEGER,
                IsVip INTEGER,
                PRIMARY KEY (ChannelID, UserID)
            );";

        ExecuteNonQuery(connection, createChannelsTable);
        ExecuteNonQuery(connection, createFirstMessagesTable);
    }

    static void ProcessChannelCoreData(SQLiteConnection connection, SQLiteTransaction transaction,
        string platform, string channelId, string channelDir)
    {
        string cddPath = Path.Combine(channelDir, "CDD.json");
        string banwordsPath = Path.Combine(channelDir, "BANWORDS.json");

        string cddData = null;
        string banWords = null;

        // Загружаем CDD.json если существует
        if (File.Exists(cddPath))
        {
            cddData = File.ReadAllText(cddPath);
        }

        // Загружаем BANWORDS.json если существует
        if (File.Exists(banwordsPath))
        {
            banWords = File.ReadAllText(banwordsPath);
        }

        string safePlatform = SanitizeTableName(platform);
        string insertSql = $@"
            INSERT OR REPLACE INTO [{safePlatform}] (ChannelID, CDDData, BanWords)
            VALUES (@ChannelID, @CDDData, @BanWords);";

        using (var cmd = new SQLiteCommand(insertSql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("@ChannelID", channelId);
            cmd.Parameters.AddWithValue("@CDDData", cddData ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BanWords", banWords ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    static void ProcessFirstMessages(SQLiteConnection connection, SQLiteTransaction transaction,
        string platform, string channelId, string fmDir)
    {
        if (!Directory.Exists(fmDir))
        {
            return;
        }

        int processed = 0;
        int errors = 0;
        string safePlatform = SanitizeTableName(platform);

        foreach (var file in Directory.GetFiles(fmDir, "*.json"))
        {
            try
            {
                string userIdStr = Path.GetFileNameWithoutExtension(file);
                long userId = long.Parse(userIdStr);
                JObject data = JObject.Parse(File.ReadAllText(file));

                string insertSql = $@"
                    INSERT OR IGNORE INTO [FirstMessage_{safePlatform}] (
                        ChannelID, UserID, MessageDate, MessageText,
                        IsMe, IsModerator, IsSubscriber, IsPartner, IsStaff, IsTurbo, IsVip
                    ) VALUES (
                        @ChannelID, @UserID, @MessageDate, @MessageText,
                        @IsMe, @IsModerator, @IsSubscriber, @IsPartner, @IsStaff, @IsTurbo, @IsVip
                    );";

                using (var cmd = new SQLiteCommand(insertSql, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@ChannelID", channelId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@MessageDate", GetNullableString(data["messageDate"]));
                    cmd.Parameters.AddWithValue("@MessageText", GetNullableString(data["messageText"]));
                    cmd.Parameters.AddWithValue("@IsMe", GetBoolAsInt(data["isMe"]));
                    cmd.Parameters.AddWithValue("@IsModerator", GetBoolAsInt(data["isModerator"]));
                    cmd.Parameters.AddWithValue("@IsSubscriber", GetBoolAsInt(data["isSubscriber"]));
                    cmd.Parameters.AddWithValue("@IsPartner", GetBoolAsInt(data["isPartner"]));
                    cmd.Parameters.AddWithValue("@IsStaff", GetBoolAsInt(data["isStaff"]));
                    cmd.Parameters.AddWithValue("@IsTurbo", GetBoolAsInt(data["isTurbo"]));
                    cmd.Parameters.AddWithValue("@IsVip", GetBoolAsInt(data["isVip"]));
                    cmd.ExecuteNonQuery();
                }

                processed++;
            }
            catch
            {
                errors++;
            }
        }
    }

    static void ProcessUserMessages(SQLiteConnection connection, SQLiteTransaction transaction,
        string platform, string channelId, string msgsDir)
    {
        if (!Directory.Exists(msgsDir))
        {
            return;
        }

        int processedFiles = 0;
        int processedMessages = 0;
        int errors = 0;
        string safePlatform = SanitizeTableName(platform);
        string tableName = $"{safePlatform}_{SanitizeTableName(channelId)}";

        // Создаем таблицу для этого канала в Messages.db
        CreateMessageTableForChannel(connection, tableName);

        foreach (var file in Directory.GetFiles(msgsDir, "*.json"))
        {
            processedFiles++;

            try
            {
                string userIdStr = Path.GetFileNameWithoutExtension(file);
                long userId = long.Parse(userIdStr);
                JObject data = JObject.Parse(File.ReadAllText(file));

                JArray messages = (JArray)data["messages"];
                if (messages == null || messages.Count == 0) continue;

                foreach (JObject msg in messages)
                {
                    string insertSql = $@"
                        INSERT INTO [{tableName}] (
                            UserID, MessageDate, MessageText,
                            IsMe, IsModerator, IsSubscriber, IsPartner, IsStaff, IsTurbo, IsVip
                        ) VALUES (
                            @UserID, @MessageDate, @MessageText,
                            @IsMe, @IsModerator, @IsSubscriber, @IsPartner, @IsStaff, @IsTurbo, @IsVip
                        );";

                    using (var cmd = new SQLiteCommand(insertSql, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@MessageDate", GetNullableString(msg["messageDate"]));
                        cmd.Parameters.AddWithValue("@MessageText", GetNullableString(msg["messageText"]));
                        cmd.Parameters.AddWithValue("@IsMe", GetBoolAsInt(msg["isMe"]));
                        cmd.Parameters.AddWithValue("@IsModerator", GetBoolAsInt(msg["isModerator"]));
                        cmd.Parameters.AddWithValue("@IsSubscriber", GetBoolAsInt(msg["isSubscriber"]));
                        cmd.Parameters.AddWithValue("@IsPartner", GetBoolAsInt(msg["isPartner"]));
                        cmd.Parameters.AddWithValue("@IsStaff", GetBoolAsInt(msg["isStaff"]));
                        cmd.Parameters.AddWithValue("@IsTurbo", GetBoolAsInt(msg["isTurbo"]));
                        cmd.Parameters.AddWithValue("@IsVip", GetBoolAsInt(msg["isVip"]));
                        cmd.ExecuteNonQuery();
                    }

                    processedMessages++;
                }
            }
            catch
            {
                errors++;
            }
        }
    }

    static void CreateMessageTableForChannel(SQLiteConnection connection, string tableName)
    {
        string createTableSql = $@"
            CREATE TABLE IF NOT EXISTS [{tableName}] (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UserID INTEGER NOT NULL,
                MessageDate TEXT,
                MessageText TEXT,
                IsMe INTEGER,
                IsModerator INTEGER,
                IsSubscriber INTEGER,
                IsPartner INTEGER,
                IsStaff INTEGER,
                IsTurbo INTEGER,
                IsVip INTEGER
            );";

        ExecuteNonQuery(connection, createTableSql);
    }

    // ========== НОВАЯ ЧАСТЬ: Миграция игровых данных ==========
    static void MigrateGames(SQLiteConnection connection, string gamesRoot)
    {
        Console.WriteLine($"Источник: {gamesRoot}");
        Console.WriteLine("Структура базы: Games.db с таблицами для каждой игры");

        // Создаем необходимые таблицы
        CreateGamesTables(connection);

        int totalPlatforms = 0;
        int totalErrors = 0;

        foreach (var platformPath in Directory.GetDirectories(gamesRoot))
        {
            string platformName = new DirectoryInfo(platformPath).Name;
            totalPlatforms++;

            Console.WriteLine($"\n{new string('-', 40)}");
            Console.WriteLine($"ОБРАБОТКА ПЛАТФОРМЫ: {platformName}");
            Console.WriteLine(new string('-', 40));

            // Миграция данных печенья
            string cookiesPath = Path.Combine(platformPath, "COOKIES");
            if (Directory.Exists(cookiesPath))
            {
                Console.WriteLine("  Миграция данных печенья:");
                MigrateCookiesData(connection, platformName, cookiesPath);
            }

            // Миграция данных лягушек
            string frogsPath = Path.Combine(platformPath, "FROGS");
            if (Directory.Exists(frogsPath))
            {
                Console.WriteLine("  Миграция данных лягушек:");
                MigrateFrogsData(connection, platformName, frogsPath);
            }
        }

        Console.WriteLine($"\nИТОГИ МИГРАЦИИ ИГРОВЫХ ДАННЫХ:");
        Console.WriteLine($"  Обработано платформ: {totalPlatforms}");
        Console.WriteLine($"  Ошибок: {totalErrors}");
    }

    static void CreateGamesTables(SQLiteConnection connection)
    {
        // Таблица для топа печенья
        string createCookiesTable = @"
            CREATE TABLE IF NOT EXISTS CookiesLeaderboard (
                Platform TEXT NOT NULL,
                UserID INTEGER NOT NULL,
                EatersCount INTEGER DEFAULT 0,
                GiftersCount INTEGER DEFAULT 0,
                RecipientsCount INTEGER DEFAULT 0,
                PRIMARY KEY (Platform, UserID)
            );";

        // Таблица для данных лягушек
        string createFrogsTable = @"
            CREATE TABLE IF NOT EXISTS Frogs (
                Platform TEXT NOT NULL,
                UserID INTEGER PRIMARY KEY,
                Frogs INTEGER DEFAULT 0,
                Gifted INTEGER DEFAULT 0,
                Received INTEGER DEFAULT 0
            );";

        ExecuteNonQuery(connection, createCookiesTable);
        ExecuteNonQuery(connection, createFrogsTable);
    }

    static void MigrateCookiesData(SQLiteConnection connection, string platform, string cookiesPath)
    {
        string topPath = Path.Combine(cookiesPath, "TOP.js");
        if (!File.Exists(topPath))
        {
            Console.WriteLine("    • TOP.js не найден, пропускаем");
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(topPath);
            JObject data = JObject.Parse(jsonData);

            // Собираем все UserID из всех leaderboard'ов
            var allUserIds = new HashSet<string>();

            if (data["leaderboard_eaters"] != null)
                foreach (var item in data["leaderboard_eaters"])
                    allUserIds.Add(item.Path);

            if (data["leaderboard_gifters"] != null)
                foreach (var item in data["leaderboard_gifters"])
                    allUserIds.Add(item.Path);

            if (data["leaderboard_recipients"] != null)
                foreach (var item in data["leaderboard_recipients"])
                    allUserIds.Add(item.Path);

            // Обрабатываем каждого пользователя
            int processed = 0;
            using (var transaction = connection.BeginTransaction())
            {
                foreach (string userIdStr in allUserIds)
                {
                    if (!long.TryParse(userIdStr, out long userId)) continue;

                    long eatersCount = GetIntValue(data["leaderboard_eaters"]?[userIdStr]);
                    long giftersCount = GetIntValue(data["leaderboard_gifters"]?[userIdStr]);
                    long recipientsCount = GetIntValue(data["leaderboard_recipients"]?[userIdStr]);

                    InsertOrUpdateCookiesLeaderboard(connection, transaction,
                        platform, userId, eatersCount, giftersCount, recipientsCount);

                    processed++;
                }
                transaction.Commit();
            }

            Console.WriteLine($"    • Обработано игроков: {processed}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ! Ошибка при обработке TOP.js: {ex.Message}");
        }
    }

    static void MigrateFrogsData(SQLiteConnection connection, string platform, string frogsPath)
    {
        int processed = 0;
        int errors = 0;

        using (var transaction = connection.BeginTransaction())
        {
            foreach (var file in Directory.GetFiles(frogsPath, "*.json"))
            {
                try
                {
                    string userIdStr = Path.GetFileNameWithoutExtension(file);
                    if (!long.TryParse(userIdStr, out long userId)) continue;

                    JObject data = JObject.Parse(File.ReadAllText(file));

                    long frogs = GetIntValue(data["frogs"]);
                    long gifted = GetIntValue(data["gifted"]);
                    long received = GetIntValue(data["received"]);

                    InsertOrUpdateFrogsData(connection, transaction,
                        platform, userId, frogs, gifted, received);

                    processed++;
                }
                catch
                {
                    errors++;
                }
            }
            transaction.Commit();
        }

        Console.WriteLine($"    • Обработано игроков: {processed} | Ошибок: {errors}");
    }

    static void InsertOrUpdateCookiesLeaderboard(SQLiteConnection connection, SQLiteTransaction transaction,
        string platform, long userId, long eatersCount, long giftersCount, long recipientsCount)
    {
        string upsertSql = @"
            INSERT OR REPLACE INTO CookiesLeaderboard 
                (Platform, UserID, EatersCount, GiftersCount, RecipientsCount)
            VALUES 
                (@Platform, @UserID, 
                 COALESCE((SELECT EatersCount FROM CookiesLeaderboard WHERE Platform = @Platform AND UserID = @UserID), 0) + @EatersCount,
                 COALESCE((SELECT GiftersCount FROM CookiesLeaderboard WHERE Platform = @Platform AND UserID = @UserID), 0) + @GiftersCount,
                 COALESCE((SELECT RecipientsCount FROM CookiesLeaderboard WHERE Platform = @Platform AND UserID = @UserID), 0) + @RecipientsCount);";

        using (var cmd = new SQLiteCommand(upsertSql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("@Platform", platform);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@EatersCount", eatersCount);
            cmd.Parameters.AddWithValue("@GiftersCount", giftersCount);
            cmd.Parameters.AddWithValue("@RecipientsCount", recipientsCount);
            cmd.ExecuteNonQuery();
        }
    }

    static void InsertOrUpdateFrogsData(SQLiteConnection connection, SQLiteTransaction transaction,
        string platform, long userId, long frogs, long gifted, long received)
    {
        string upsertSql = @"
            INSERT OR REPLACE INTO Frogs 
                (Platform, UserID, Frogs, Gifted, Received)
            VALUES 
                (@Platform, @UserID,
                 COALESCE((SELECT Frogs FROM Frogs WHERE Platform = @Platform AND UserID = @UserID), 0) + @Frogs,
                 COALESCE((SELECT Gifted FROM Frogs WHERE Platform = @Platform AND UserID = @UserID), 0) + @Gifted,
                 COALESCE((SELECT Received FROM Frogs WHERE Platform = @Platform AND UserID = @UserID), 0) + @Received);";

        using (var cmd = new SQLiteCommand(upsertSql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("@Platform", platform);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@Frogs", frogs);
            cmd.Parameters.AddWithValue("@Gifted", gifted);
            cmd.Parameters.AddWithValue("@Received", received);
            cmd.ExecuteNonQuery();
        }
    }

    // ========== НОВЫЙ МЕТОД: Сбор статистики сообщений по каналам ==========
    static void MigrateChannelMessagesCount(SQLiteConnection usersConnection, string channelsRoot)
    {
        if (!Directory.Exists(channelsRoot))
        {
            Console.WriteLine("Папка CHNLS не найдена. Пропускаем сбор статистики сообщений по каналам.");
            return;
        }

        Console.WriteLine("\n" + new string('-', 50));
        Console.WriteLine("СБОР СТАТИСТИКИ СООБЩЕНИЙ ПО КАНАЛАМ (ChannelMessagesCount)");
        Console.WriteLine(new string('-', 50));

        // Словарь для сбора статистики: [платформа][userId] = словарь {channelId: count}
        var allStats = new Dictionary<string, Dictionary<long, Dictionary<string, int>>>();

        // 1. Собираем статистику из всех каналов
        foreach (var platformPath in Directory.GetDirectories(channelsRoot))
        {
            string platformName = new DirectoryInfo(platformPath).Name;
            allStats[platformName] = new Dictionary<long, Dictionary<string, int>>();

            foreach (var channelPath in Directory.GetDirectories(platformPath))
            {
                string channelId = new DirectoryInfo(channelPath).Name;
                string msDir = Path.Combine(channelPath, "MS");

                if (!Directory.Exists(msDir)) continue;

                foreach (var countFile in Directory.GetFiles(msDir, "*.txt"))
                {
                    string userIdStr = Path.GetFileNameWithoutExtension(countFile);
                    if (!long.TryParse(userIdStr, out long userId)) continue;

                    try
                    {
                        string countText = File.ReadAllText(countFile).Trim();
                        if (!int.TryParse(countText, out int count)) continue;

                        // Инициализируем запись пользователя если нужно
                        if (!allStats[platformName].ContainsKey(userId))
                        {
                            allStats[platformName][userId] = new Dictionary<string, int>();
                        }

                        // Добавляем статистику для канала
                        allStats[platformName][userId][channelId] = count;
                    }
                    catch { /* Игнорируем ошибки */ }
                }
            }
        }

        // 2. Обновляем записи пользователей
        int totalUpdated = 0;
        int totalErrors = 0;

        foreach (var platformEntry in allStats)
        {
            string platform = platformEntry.Key;
            string safeTableName = SanitizeTableName(platform);

            foreach (var userEntry in platformEntry.Value)
            {
                long userId = userEntry.Key;
                var channelStats = userEntry.Value;

                try
                {
                    // Сериализуем статистику в JSON
                    string jsonStats = JsonConvert.SerializeObject(channelStats);

                    string updateSql = $@"
                    UPDATE ""{safeTableName}""
                    SET ChannelMessagesCount = @JsonStats
                    WHERE ID = @UserId;";

                    using (var cmd = new SQLiteCommand(updateSql, usersConnection))
                    {
                        cmd.Parameters.AddWithValue("@JsonStats", jsonStats);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    totalUpdated++;
                }
                catch
                {
                    totalErrors++;
                }
            }
        }

        Console.WriteLine($"\nИтоги обновления ChannelMessagesCount:");
        Console.WriteLine($"  Обновлено записей: {totalUpdated}");
        Console.WriteLine($"  Ошибок: {totalErrors}");
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========
    static string SanitizeTableName(string name)
    {
        // Удаляем все недопустимые символы из названия таблицы
        string safeName = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");

        // Убедимся, что имя не пустое
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "Unknown";

        // Ограничиваем длину имени таблицы
        if (safeName.Length > 50)
            safeName = safeName.Substring(0, 50);

        return safeName;
    }

    static long GetIntValue(JToken token)
    {
        if (token == null) return 0;

        if (token.Type == JTokenType.Integer)
            return (int)token;

        if (long.TryParse(token.ToString(), out long result))
            return result;

        return 0;
    }

    static string GetNullableString(JToken token)
    {
        return token?.ToString() ?? DBNull.Value.ToString();
    }

    static object GetNullableInt(JToken token)
    {
        if (token == null) return DBNull.Value;

        if (token.Type == JTokenType.Integer)
            return (int)token;

        if (long.TryParse(token.ToString(), out long result))
            return result;

        return DBNull.Value;
    }

    static int GetIsAfkValue(JToken token)
    {
        if (token == null) return 0;
        return token.Type == JTokenType.Boolean && (bool)token ? 1 : 0;
    }

    static int GetBoolAsInt(JToken token)
    {
        if (token == null) return 0;
        return token.Type == JTokenType.Boolean && (bool)token ? 1 : 0;
    }

    static string ProcessLanguage(string lang)
    {
        return lang switch
        {
            "ru" => "ru-RU",
            "en" => "en-US",
            _ => lang ?? DBNull.Value.ToString()
        };
    }

    static void ExecuteNonQuery(SQLiteConnection connection, string sql)
    {
        using (var cmd = new SQLiteCommand(sql, connection))
        {
            cmd.ExecuteNonQuery();
        }
    }
}