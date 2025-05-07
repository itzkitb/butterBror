using Android.Content.Res;
using Android.Views;
using butterBror;
using butterBror.Utils;

namespace butterBror_android
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private static TextView title;
        private static TextView console;
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            title = FindViewById<TextView>(Resource.Id.console_title);
            console = FindViewById<TextView>(Resource.Id.console);

            butterBror.Utils.Console.on_chat_line += OnChatLineGetted;
            butterBror.Utils.Console.error_occured += OnErrorOccured;
            Engine.Start();
            System.Console.ReadLine();
        }

        private static void OnChatLineGetted(butterBror.Utils.Console.LineInfo line)
        {
            if (!new string[] { "files", "status" }.Contains(line.Channel))
                console.Text += $"[ {line.Channel} ]{line.Message}";
            else if (line.Channel == "status")
                title.Text = $"butterBror | {line.Message}";
        }

        private static void OnErrorOccured(butterBror.Utils.Console.LineInfo line)
        {
            console.Text += $"[ {line.Channel} ]{line.Message}";
        }
    }
}