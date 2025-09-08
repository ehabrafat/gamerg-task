using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class ProcessResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Memory { get; set; }
        public DateTime StartTime { get; set; }
        public bool RespondingStatus { get; set; }
    }
    public partial class MainWindow : Window
    {
        public const string LogsDirName = "Logs";
        public ObservableCollection<ProcessResponse> Processes { get; set; } = new();

        private List<ProcessResponse> GetCurrentProcesses()
        {
           return Process.GetProcesses()
                .Select(x => new ProcessResponse
                {
                    Id = x.Id,
                    Name = x.ProcessName,
                    Memory = x.WorkingSet64 / (1024 * 1024), // MB
                    StartTime = DateTime.Now, // x.StartTime,
                    RespondingStatus = x.Responding
                })
                .OrderBy(x => x.Name)
                .ToList();
        }

        private async Task RefreshProcesses()
        {
            var newProcesses = await Task.Run(GetCurrentProcesses);
            Processes.Clear();
            newProcesses.ForEach(process => Processes.Add(process));
            await SaveToFile();
        }

        private async Task SaveToFile()
        {
            var logsDir = Path.Join(Directory.GetCurrentDirectory(), LogsDirName);
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logFilePath = Path.Combine(logsDir, $"processes_{timestamp}.log");
            var content = string.Join(Environment.NewLine, Processes.Select(p => 
                $"ID: {p.Id}, Name: {p.Name}, Memory: {p.Memory} MB, StartTime: {p.StartTime}, Responding: {p.RespondingStatus}"));
        
            await File.WriteAllTextAsync(logFilePath, content);
        }
        private async Task ProcessRefreshWorker()
        {
            while (true)
            {
                await RefreshProcesses();
                await Task.Delay(5000);
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _ = ProcessRefreshWorker();
        }

        private async void Refresh_OnClick(object sender, RoutedEventArgs e)
        {
            await RefreshProcesses();
        }
    }
}