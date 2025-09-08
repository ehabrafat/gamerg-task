using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        public ObservableCollection<ProcessResponse> Processes { get; set; } = new();

        public void RefreshProcesses()
        {
            var res = Process.GetProcesses()
                .Select(x => new ProcessResponse
                {
                    Id = x.Id,
                    Name = x.ProcessName,
                    Memory = x.WorkingSet64 / (1024 * 1024), // MB
                    StartTime = DateTime.Now, // x.StartTime
                    RespondingStatus = x.Responding
                }).OrderBy(x => x.Name)
            .ToList();
            
            Processes.Clear();
            foreach (var x in res) Processes.Add(x);
        }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _ = RefreshProccess();
        }

        public async Task RefreshProccess()
        {
            while (true)
            {
                RefreshProcesses();
                await Task.Delay(2000);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcesses();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
         
                e.Cancel = true;
                Hide();
            
        }
    }
}