using butterBror;
using butterBror.Utils;

class Programm
{
    public static void Main(string[] args)
    {
        butterBror.Utils.Console.on_chat_line += OnChatLineGetted;
        butterBror.Utils.Console.error_occured += OnErrorOccured;
        Engine.Start();
        System.Console.ReadLine();
    }

    private static void OnChatLineGetted(butterBror.Utils.Console.LineInfo line)
    {
        if (!new string[] { "cafus", "status" }.Contains(line.Channel))
            System.Console.Write($"[ {line.Channel} ]{line.Message}");
        else if (line.Channel == "status")
            System.Console.Title = "butterBror | " + line.Message;
    }

    private static void OnErrorOccured(butterBror.Utils.Console.LineInfo line)
    {
        System.Console.ForegroundColor = ConsoleColor.Black;
        System.Console.BackgroundColor = ConsoleColor.Red;
        System.Console.Write($"[ {line.Channel} ]{line.Message}");
        System.Console.ResetColor();
    }
}