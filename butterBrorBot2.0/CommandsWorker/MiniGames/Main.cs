using butterBror.Utils;
using butterBror.Utils.DataManagers;

namespace butterBror
{
    public partial class MiniGames
    {
        public class Main
        {
            private static Dictionary<string, Videocard> videocards = new Dictionary<string, Videocard>();
            private static Dictionary<string, Processor> processors = new Dictionary<string, Processor>();

            public static void BuyVideocard(string userId, string videocardId)
            {
                if (videocards.ContainsKey(videocardId))
                {
                    // Проверка баланса пользователя
                    int price = videocards[videocardId].Price;
                    if (CheckBalance(userId, price))
                    {
                        // Добавление видеокарты к пользователю
                        UsersData.UserSaveData(userId, "miningVideocards", videocardId);
                        // Вычитание стоимости из баланса
                        BalanceUtil.SaveBalance(userId, 0, -price);
                    }
                }
            }

            public static void BuyProcessor(string userId, string processorId)
            {
                if (processors.ContainsKey(processorId))
                {
                    // Проверка баланса пользователя
                    int price = processors[processorId].Price;
                    if (CheckBalance(userId, price))
                    {
                        // Добавление процессора к пользователю
                        UsersData.UserSaveData(userId, "miningProcessors", processorId);
                        // Вычитание стоимости из баланса
                        BalanceUtil.SaveBalance(userId, 0, -price);
                    }
                }
            }

            public static void CollectMiningIncome(string userId, Dictionary<string, int> miningInventory)
            {
                DateTime lastMiningClear = UsersData.UserGetData<DateTime>(userId, "lastMiningClear");
                DateTime currentTime = DateTime.Now;
                TimeSpan timeElapsed = currentTime - lastMiningClear;

                int totalIncome = 0;

                foreach (var item in miningInventory)
                {
                    if (item.Key.StartsWith("videocard"))
                    {
                        Videocard videocard = videocards[item.Key];
                        int incomePerMinute = videocard.IncomePerMinute;
                        totalIncome += incomePerMinute * (int)timeElapsed.TotalMinutes * item.Value;
                    }
                    else if (item.Key.StartsWith("processor"))
                    {
                        Processor processor = processors[item.Key];
                        int incomePerMinute = processor.IncomePerMinute;
                        totalIncome += incomePerMinute * (int)timeElapsed.TotalMinutes * item.Value;
                    }
                }

                // Добавление заработанной суммы к балансу пользователя
                BalanceUtil.SaveBalance(userId, 0, totalIncome);
                // Обновление времени последнего сбора
                UsersData.UserSaveData(userId, "lastMiningClear", currentTime);
            }

            private static bool CheckBalance(string userId, int amount)
            {
                int balance = UsersData.UserGetData<int>(userId, "floatBalance");
                balance += UsersData.UserGetData<int>(userId, "balance") * 100;
                if (balance >= amount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // #MININGADDHARD
            // Метод для добавления видеокарт и процессоров
            public static void AddHardware()
            {
                try
                {
                    videocards.Add("Intel HD Graphics 4000", new Videocard("Intel HD Graphics 4000", 10000, 1)); // 1
                    videocards.Add("AMD Radeon R5 310", new Videocard("AMD Radeon R5 310", 11000, 2)); // 2
                    videocards.Add("Nvidia GeForce 210", new Videocard("Nvidia GeForce 210", 12000, 3));
                    videocards.Add("Intel UHD Graphics 620", new Videocard("Intel UHD Graphics 620", 13000, 4));
                    videocards.Add("AMD Radeon R5 330", new Videocard("AMD Radeon R5 330", 14000, 5));
                    videocards.Add("Nvidia GeForce GT 710", new Videocard("Nvidia GeForce GT 710", 15000, 6));
                    videocards.Add("Intel UHD Graphics 630", new Videocard("Intel UHD Graphics 630", 16000, 7));
                    videocards.Add("AMD Radeon R7 240", new Videocard("AMD Radeon R7 240", 17000, 8));
                    videocards.Add("Nvidia GeForce GT 720", new Videocard("Nvidia GeForce GT 720", 18000, 9));
                    videocards.Add("Intel Iris Plus Graphics 640", new Videocard("Intel Iris Plus Graphics 640", 19000, 10));
                    videocards.Add("AMD Radeon R7 250", new Videocard("AMD Radeon R7 250", 20000, 11));
                    videocards.Add("Nvidia GeForce GT 730", new Videocard("Nvidia GeForce GT 730", 21000, 12));
                    videocards.Add("Intel Iris Plus Graphics 650", new Videocard("Intel Iris Plus Graphics 650", 22000, 13));
                    videocards.Add("AMD Radeon R7 260", new Videocard("AMD Radeon R7 260", 23000, 14));
                    videocards.Add("Nvidia GeForce GT 740", new Videocard("Nvidia GeForce GT 740", 24000, 15));
                    videocards.Add("Intel Iris Plus Graphics 655", new Videocard("Intel Iris Plus Graphics 655", 25000, 16));
                    videocards.Add("AMD Radeon R7 360", new Videocard("AMD Radeon R7 360", 26000, 17));
                    videocards.Add("Nvidia GeForce GT 1030", new Videocard("Nvidia GeForce GT 1030", 27000, 18));
                    videocards.Add("Intel Iris Plus Graphics 675", new Videocard("Intel Iris Plus Graphics 675", 28000, 19));
                    videocards.Add("AMD Radeon R7 370", new Videocard("AMD Radeon R7 370", 29000, 20));
                    videocards.Add("Nvidia GeForce GTX 750", new Videocard("Nvidia GeForce GTX 750", 30000, 21));
                    videocards.Add("AMD Radeon R9 270X", new Videocard("AMD Radeon R9 270X", 32000, 23));
                    videocards.Add("Nvidia GeForce GTX 750 Ti", new Videocard("Nvidia GeForce GTX 750 Ti", 46500, 24));
                    videocards.Add("AMD Radeon RX 460", new Videocard("AMD Radeon RX 460", 64000, 26));
                    videocards.Add("Nvidia GeForce GTX 760", new Videocard("Nvidia GeForce GTX 760", 76000, 27));
                    videocards.Add("Intel UHD Graphics 750", new Videocard("Intel UHD Graphics 750", 80000, 28));
                    videocards.Add("AMD Radeon RX 550", new Videocard("AMD Radeon RX 550", 87000, 29));
                    videocards.Add("Nvidia GeForce GTX 950", new Videocard("Nvidia GeForce GTX 950", 98000, 30));
                    videocards.Add("AMD Radeon RX 560", new Videocard("AMD Radeon RX 560", 118900, 32));
                    videocards.Add("Nvidia GeForce GTX 960", new Videocard("Nvidia GeForce GTX 960", 120000, 33));
                    videocards.Add("Intel Iris Xe Graphics", new Videocard("Intel Iris Xe Graphics", 123400, 34));
                    videocards.Add("AMD Radeon RX 570", new Videocard("AMD Radeon RX 570", 126000, 35));
                    videocards.Add("Nvidia GeForce GTX 1050", new Videocard("Nvidia GeForce GTX 1050", 130400, 36));
                    videocards.Add("Intel Iris Xe MAX Graphics", new Videocard("Intel Iris Xe MAX Graphics", 136000, 37));
                    videocards.Add("AMD Radeon RX 580", new Videocard("AMD Radeon RX 580", 136700, 38));
                    videocards.Add("Nvidia GeForce GTX 1050 Ti", new Videocard("Nvidia GeForce GTX 1050 Ti", 140000, 39));
                    videocards.Add("AMD Radeon RX 590", new Videocard("AMD Radeon RX 590", 145600, 40));
                    videocards.Add("Nvidia GeForce GTX 1060", new Videocard("Nvidia GeForce GTX 1060", 156700, 41));
                    videocards.Add("AMD Radeon RX 6500 XT", new Videocard("AMD Radeon RX 6500 XT", 165400, 42));
                    videocards.Add("Nvidia GeForce GTX 1650", new Videocard("Nvidia GeForce GTX 1650", 179000, 43));
                    videocards.Add("AMD Radeon RX 6600 XT", new Videocard("AMD Radeon RX 6600 XT", 198400, 44));
                    videocards.Add("Nvidia GeForce GTX 1660", new Videocard("Nvidia GeForce GTX 1660", 200900, 45));
                    videocards.Add("AMD Radeon RX 6700 XT", new Videocard("AMD Radeon RX 6700 XT", 220200, 46));
                    videocards.Add("Nvidia GeForce GTX 1660 Super", new Videocard("Nvidia GeForce GTX 1660 Super", 235600, 47));
                    videocards.Add("AMD Radeon RX 6800", new Videocard("AMD Radeon RX 6800", 257000, 48));
                    videocards.Add("Nvidia GeForce GTX 1660 Ti", new Videocard("Nvidia GeForce GTX 1660 Ti", 258900, 49));
                    videocards.Add("AMD Radeon RX 6900 XT", new Videocard("AMD Radeon RX 6900 XT", 289000, 50));

                    processors.Add("Intel Celeron G4920", new Processor("Intel Celeron G4920", 10000, 1));
                    processors.Add("AMD Sempron 2650", new Processor("AMD Sempron 2650", 11000, 2));
                    processors.Add("Intel Pentium Gold G5400", new Processor("Intel Pentium Gold G5400", 12000, 3));
                    processors.Add("AMD Athlon 200GE", new Processor("AMD Athlon 200GE", 13000, 4));
                    processors.Add("Intel Core i3-8100", new Processor("Intel Core i3-8100", 14000, 5));
                    processors.Add("AMD Ryzen 3 2200G", new Processor("AMD Ryzen 3 2200G", 15000, 6));
                    processors.Add("Intel Core i5-8400", new Processor("Intel Core i5-8400", 16000, 7));
                    processors.Add("AMD Ryzen 5 2400G", new Processor("AMD Ryzen 5 2400G", 17000, 8));
                    processors.Add("Intel Core i5-9600K", new Processor("Intel Core i5-9600K", 18000, 9));
                    processors.Add("AMD Ryzen 5 3400G", new Processor("AMD Ryzen 5 3400G", 19000, 10));
                    processors.Add("Intel Core i5-10600K", new Processor("Intel Core i5-10600K", 20000, 11));
                    processors.Add("AMD Ryzen 5 3600", new Processor("AMD Ryzen 5 3600", 21000, 12));
                    processors.Add("Intel Core i7-8700K", new Processor("Intel Core i7-8700K", 22000, 13));
                    processors.Add("AMD Ryzen 5 3600X", new Processor("AMD Ryzen 5 3600X", 23000, 14));
                    processors.Add("Intel Core i7-9700K", new Processor("Intel Core i7-9700K", 24000, 15));
                    processors.Add("AMD Ryzen 7 3700X", new Processor("AMD Ryzen 7 3700X", 25000, 16));
                    processors.Add("Intel Core i9-9900K", new Processor("Intel Core i9-9900K", 26000, 17));
                    processors.Add("AMD Ryzen 7 3800X", new Processor("AMD Ryzen 7 3800X", 27000, 18));
                    processors.Add("Intel Core i9-10900K", new Processor("Intel Core i9-10900K", 28000, 19));
                    processors.Add("AMD Ryzen 7 5800X", new Processor("AMD Ryzen 7 5800X", 29000, 20));
                    processors.Add("Intel Core i9-11900K", new Processor("Intel Core i9-11900K", 30000, 21));
                    processors.Add("AMD Ryzen 7 5900X", new Processor("AMD Ryzen 7 5900X", 31000, 22));
                    processors.Add("Intel Core i9-12900K", new Processor("Intel Core i9-12900K", 32000, 23));
                    processors.Add("AMD Ryzen 9 3900X", new Processor("AMD Ryzen 9 3900X", 33000, 24));
                    processors.Add("Intel Core i3-10100", new Processor("Intel Core i3-10100", 37000, 25));
                    processors.Add("AMD Ryzen 9 3900XT", new Processor("AMD Ryzen 9 3900XT", 39000, 26));
                    processors.Add("Intel Core i5-10400", new Processor("Intel Core i5-10400", 43000, 27));
                    processors.Add("AMD Ryzen 9 3950X", new Processor("AMD Ryzen 9 3950X", 47000, 28));
                    processors.Add("Intel Core i5-11400", new Processor("Intel Core i5-11400", 52000, 29));
                    processors.Add("AMD Ryzen 9 3950XT", new Processor("AMD Ryzen 9 3950XT", 59000, 30));
                    processors.Add("Intel Core i7-10700", new Processor("Intel Core i7-10700", 60000, 31));
                    processors.Add("AMD Ryzen 9 5900X", new Processor("AMD Ryzen 9 5900X", 61000, 32));
                    processors.Add("Intel Core i7-11700", new Processor("Intel Core i7-11700", 62000, 33));
                    processors.Add("AMD Ryzen 9 5950X", new Processor("AMD Ryzen 9 5950X", 63000, 34));
                    processors.Add("Intel Core i9-10850K", new Processor("Intel Core i9-10850K", 64000, 35));
                    processors.Add("AMD Ryzen Threadripper 3960X", new Processor("AMD Ryzen Threadripper 3960X", 69000, 36));
                    processors.Add("Intel Core i9-10980XE", new Processor("Intel Core i9-10980XE", 70000, 37));
                    processors.Add("AMD Ryzen Threadripper 3970X", new Processor("AMD Ryzen Threadripper 3970X", 75000, 38));
                    processors.Add("Intel Core i9-11900XE", new Processor("Intel Core i9-11900XE", 77000, 39));
                    processors.Add("AMD Ryzen Threadripper 3990X", new Processor("AMD Ryzen Threadripper 3990X", 79000, 40));
                    processors.Add("Intel Core i9-12900XE", new Processor("Intel Core i9-12900XE", 80000, 41));
                    processors.Add("AMD EPYC 7702", new Processor("AMD EPYC 7702", 83000, 42));
                    processors.Add("Intel Xeon W-3275", new Processor("Intel Xeon W-3275", 94000, 43));
                    processors.Add("AMD EPYC 7742", new Processor("AMD EPYC 7742", 97000, 44));
                    processors.Add("Intel Xeon Gold 6258R", new Processor("Intel Xeon Gold 6258R", 109999, 45));
                    processors.Add("AMD EPYC 7763", new Processor("AMD EPYC 7763", 142300, 46));
                    processors.Add("Intel Xeon Platinum 8260", new Processor("Intel Xeon Platinum 8260", 152500, 47));
                    processors.Add("AMD EPYC 77F3", new Processor("AMD EPYC 77F3", 180000, 48));
                    processors.Add("Intel Xeon Platinum 8284", new Processor("Intel Xeon Platinum 8284", 245000, 49));
                    processors.Add("AMD EPYC 75F3", new Processor("AMD EPYC 75F3", 249000, 50));
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "MININGADDHARD");
                }
            }
        }

        public class Videocard
        {
            public string Name { get; set; }
            public int Price { get; set; }
            public int IncomePerMinute { get; set; }

            public Videocard(string name, int price, int incomePerMinute)
            {
                Name = name;
                Price = price;
                IncomePerMinute = incomePerMinute;
            }
        }

        public class Processor
        {
            public string Name { get; set; }
            public int Price { get; set; }
            public int IncomePerMinute { get; set; }

            public Processor(string name, int price, int incomePerMinute)
            {
                Name = name;
                Price = price;
                IncomePerMinute = incomePerMinute;
            }
        }
    }
}
