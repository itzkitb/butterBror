using System;
using System.Diagnostics;
using butterBror;
using butterBror.Utils;
using DankDB;

namespace butterBror___desktop
{
    public partial class Form1 : Form
    {
        private SmoothLineChart cache_read_chart = Create();
        private SmoothLineChart cache_write_chart = Create();
        private SmoothLineChart files_read_chart = Create();
        private SmoothLineChart files_write_chart = Create();
        private SmoothLineChart files_checks_chart = Create();
        private SmoothLineChart operations_chart = Create();
        private SmoothLineChart db_operations_chart = Create();
        private SmoothLineChart messages_chart = Create();
        private SmoothLineChart cpu_status_chart = Create();
        private SmoothLineChart ram_status_chart = Create();

        private List<int> cache_read_list = new List<int>();
        private List<int> cache_write_list = new List<int>();
        private List<int> files_read_list = new List<int>();
        private List<int> files_write_list = new List<int>();
        private List<int> files_checks_list = new List<int>();
        private List<int> operations_list = new List<int>();
        private List<int> db_operations_list = new List<int>();
        private List<int> messages_list = new List<int>();
        private List<int> cpu_status_list = new List<int>();
        private List<int> ram_status_list = new List<int>();

        PerformanceCounter cpu_counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        private static Dictionary<string, Label> consoles = new();

        private System.Threading.Timer update_timer;

        public Form1()
        {
            InitializeComponent();

            this.Text = "butterBror";

            cache_reads.Controls.Add(cache_read_chart);
            cache_writes.Controls.Add(cache_write_chart);
            files_read.Controls.Add(files_read_chart);
            files_writes.Controls.Add(files_write_chart);
            files_checks.Controls.Add(files_checks_chart);
            operations_per_second.Controls.Add(operations_chart);
            files_operations.Controls.Add(db_operations_chart);
            messages_per_second.Controls.Add(messages_chart);
            cpu_status.Controls.Add(cpu_status_chart);
            ram_status.Controls.Add(ram_status_chart);

            version.Text = $"v. {Core.Version}{Core.Patch}";

            consoles.Add("kernel", kernel_console);
            consoles.Add("info", info_console);
            consoles.Add("err", errors_console);
            consoles.Add("main", main_console);
            consoles.Add("discord", info_console);
            consoles.Add("cafus", cafus_console);
            consoles.Add("nbw", nbw_console);

            butterBror.Utils.Console.on_chat_line += chat_line;
            butterBror.Utils.Console.error_occured += on_error;

            Core.Start();

            Main();
        }

        private void chat_line(butterBror.Utils.Console.LineInfo line)
        {
            Write(line.Channel, line.Message);
        }

        private void on_error(butterBror.Utils.Console.LineInfo line)
        {
            Write(line.Channel, line.Message + "\n");
        }

        private void Main()
        {
            System.Console.WriteLine("ÕèïîÏóé");
            update_timer = new System.Threading.Timer(TimerCallback, null, 1000, 1000);
        }

        private void TimerCallback(object state)
        {
            try
            {
                if (this.IsDisposed) return;

                long ram = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;

                this.Invoke((MethodInvoker)delegate
                {
                    AddChartPoint(20, ((int)Core.Statistics.DataBase.CacheReads.Get()), cache_read_list, cache_read_chart);
                    AddChartPoint(20, ((int)Core.Statistics.DataBase.CacheWrites.Get()), cache_write_list, cache_write_chart);
                    AddChartPoint(20, ((int)Core.Statistics.DataBase.FileReads.Get()), files_read_list, files_read_chart);
                    AddChartPoint(20, ((int)Core.Statistics.DataBase.FileWrites.Get()), files_write_list, files_write_chart);
                    AddChartPoint(20, ((int)Core.Statistics.DataBase.Checks.Get()), files_checks_list, files_checks_chart);
                    AddChartPoint(20, ((int)Core.Statistics.DataBase.Operations.Get()), db_operations_list, db_operations_chart);
                    AddChartPoint(60, Core.Statistics.FunctionsUsed.Get(), operations_list, operations_chart);
                    AddChartPoint(60, Core.Statistics.MessagesReaded.Get(), messages_list, messages_chart);
                    AddChartPoint(60, (int)ram, ram_status_list, ram_status_chart);
                    AddChartPoint(60, (int)cpu_counter.NextValue(), cpu_status_list, cpu_status_chart);
                });
            }
            catch (ObjectDisposedException)
            {
                update_timer?.Dispose();
            }
        }

        private static void Write(string console, string text)
        {
            if (consoles.TryGetValue(console, out Label label) && label != null)
            {
                if (label.InvokeRequired)
                {
                    label.Invoke(new Action(() => Write(console, text)));
                    return;
                }

                var newText = string.IsNullOrEmpty(label.Text)
                    ? text
                    : $"{label.Text}{text}";

                var lines = newText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                if (lines.Length > 250)
                {
                    newText = string.Join("", lines, lines.Length - 250, 250);
                }

                label.Text = newText;
            }
        }

        public static SmoothLineChart Create()
        {
            SmoothLineChart chart = new SmoothLineChart();
            chart.Dock = DockStyle.Fill;
            chart.LineColor = Color.FromArgb(245, 129, 66);
            chart.PointRadius = 3;
            chart.LineThickness = 1;
            chart.FPS = 1;
            chart.AnimationEnabled = false;
            return chart;
        }

        private void AddChartPoint(int maximum_points, int value, List<int> values, SmoothLineChart chart)
        {
            values.Add(value);
            if (values.Count > maximum_points)
            {
                values.RemoveAt(0);
            }
            chart.UpdateValues(values);
        }

        private void kernel_console_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(kernel_console.Text);
        }

        private void main_console_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(main_console.Text);
        }

        private void errors_console_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(errors_console.Text);
        }

        private void nbw_console_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(nbw_console.Text);
        }

        private void info_console_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(info_console.Text);
        }

        private void cafus_console_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(cafus_console.Text);
        }
    }
}
