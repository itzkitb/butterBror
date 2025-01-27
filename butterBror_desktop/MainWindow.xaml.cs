using butterBror;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace butterBror_desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, TextBlock> list = [];
        private Dictionary<string, ScrollViewer> scrollList = [];
        private Dictionary<string, List<string>> consoleList = [];
        private List<string> skipConsoles = ["files"];
        public MainWindow()
        {
            InitializeComponent();
            list.Add("main", main_console);
            list.Add("err", errors_console);
            list.Add("discord", discord_console);
            list.Add("info", commands_console);
            list.Add("tw_chat", tw_chat_console);
            list.Add("ds_chat", ds_chat_console);
            list.Add("tg_chat", tg_chat_console);
            list.Add("nbw", nbw_console);
            list.Add("files", files_console);
            list.Add("cafus", cafus_console);
            list.Add("kernel", kernel_console);

            scrollList.Add("main", scroll_main_console);
            scrollList.Add("err", scroll_errors_console);
            scrollList.Add("discord", scroll_discord_console);
            scrollList.Add("info", scroll_commands_console);
            scrollList.Add("tw_chat", scroll_tw_chat_console);
            scrollList.Add("ds_chat", scroll_ds_chat_console);
            scrollList.Add("tg_chat", scroll_tg_chat_console);
            scrollList.Add("nbw", scroll_nbw_console);
            scrollList.Add("files", scroll_files_console);
            scrollList.Add("cafus", scroll_cafus_console);
            scrollList.Add("kernel", scroll_kernel_console);

            Task.Run(() =>
            {
                ConsoleUtil.OnChatLineGetted += OnChatLine;
                ConsoleUtil.OnErrorOccured += OnError;
                BotEngine.Start();
            });
        }
        private void OnChatLine(ConsoleUtil.LogInfo line)
        {
            if (list.ContainsKey(line.Channel))
            {
                if (skipConsoles.Contains(line.Channel))
                {
                    //Debug.WriteLine(line.Channel + ": " + line.Message);
                    return;
                }
                TextBlock label = list[line.Channel];
                Dispatcher.Invoke(() =>
                {
                    Debug.Write($"[{line.Channel}] {line.Message}");
                    string channel = line.Channel;
                    if (!scrollList.TryGetValue(channel, out ScrollViewer? value)) return;
                    if (!consoleList.ContainsKey(channel))
                        consoleList[channel] = [];
                    var list = consoleList[channel];
                    list.Add(line.Message);
                    if (list.Count == 100)
                        list.RemoveAt(0);
                    bool IsScrollDown = value.VerticalOffset == value.ScrollableHeight;
                    label.Text = string.Join("", list);
                    if (IsScrollDown)
                        value.ScrollToEnd();
                    consoleList[channel] = list;
                });
            }
            else if (line.Channel == "status")
                Dispatcher.Invoke(() => { status_console.Text = line.Message; });
            else
            {
                LogWorker.Log($"Консоль \"{line.Channel}\" не найдена!", LogWorker.LogTypes.Err, "App/OnChatLine");
            }
        }

        private void OnError(ConsoleUtil.LogInfo line)
        {
            TextBlock label = list["err"];
            Dispatcher.Invoke(() =>
            {
                if (!consoleList.ContainsKey("err")) consoleList["err"] = [];
                var list = consoleList["err"];
                list.Add(line.Message);
                if (list.Count > 100)
                    list.RemoveAt(0);
                label.Text = string.Join(" ", list);
                consoleList["err"] = list;
            });
        }

        private ScrollViewer GetScrollViewer(TextBlock e)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(e);
            while (parent != null)
            {
                if (parent is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void restart_Click(object sender, RoutedEventArgs e)
        {
            Bot.Restart();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Bot.TurnOff();
        }
    }
}