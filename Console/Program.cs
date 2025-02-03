using butterBror;
using butterBror.Utils;

class Programm
{
    public static void Main(string[] args)
    {
        ConsoleUtil.OnChatLineGetted += OnChatLineGetted;
        ConsoleUtil.OnErrorOccured += OnErrorOccured;
        BotEngine.Start();
        Console.ReadLine();
    }

    private static void OnChatLineGetted(ConsoleUtil.LogInfo line)
    {
        if (!new string[]{ "files", "cafus", "status" }.Contains(line.Channel))
            Console.Write($"[ {line.Channel} ]{line.Message}");
        else if (line.Channel == "status")
            Console.Title = line.Message;
    }

    private static void OnErrorOccured(ConsoleUtil.LogInfo line)
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.Red;
        Console.Write($"[ {line.Channel} ]{line.Message}");
        Console.ResetColor();
    }
}