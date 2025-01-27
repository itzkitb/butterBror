using butterBror;
using butterBror.Utils;

class Programm
{
    public static void Main(string[] args)
    {
        ConsoleUtil.OnChatLineGetted += OnChatLineGetted;
        BotEngine.Start();
        Console.ReadLine();
    }

    private static void OnChatLineGetted(ConsoleUtil.LogInfo line)
    {
        if (line.Channel == "main")
        {
            Console.ForegroundColor = line.ForegroundColor;
            Console.BackgroundColor = line.BackgroundColor;
            Console.Write(line.Message);
            Console.ResetColor();
        }
    }
}