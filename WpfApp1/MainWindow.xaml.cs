using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using Path = System.IO.Path;

namespace WpfApp1;

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

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private TaskbarIcon? _trayIcon;
    private bool _exit;
    private CancellationTokenSource _cancellationTokenSource;

    public string TextSearch { get; set; }
    public CancellationTokenSource CancellationTokenSource
    {
        get => _cancellationTokenSource;
        set
        {
            _cancellationTokenSource = value;
            OnPropertyChanged(nameof(RefreshStopped));
            OnPropertyChanged(nameof(RefreshRunning));
            _cancellationTokenSource?.Token.Register(() =>
            {
                OnPropertyChanged(nameof(RefreshStopped));
                OnPropertyChanged(nameof(RefreshRunning));
            });
        }
    }
    public bool RefreshStopped => _cancellationTokenSource.Token.IsCancellationRequested;
    public bool RefreshRunning => !_cancellationTokenSource.Token.IsCancellationRequested;
    
    
    private const string LogsDirName = "Logs";
    public ObservableCollection<ProcessResponse> Processes { get; set; }
  
    public MainWindow()
    {
        InitializeComponent();
        CancellationTokenSource = new CancellationTokenSource();
        Processes = [];
        DataContext = this;
        _ = ProcessRefreshWorker(CancellationTokenSource.Token);
    }

    private List<ProcessResponse> GetCurrentProcesses()
    {
   
        var currentProcesses = new List<ProcessResponse>();
        
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                currentProcesses.Add(new ProcessResponse { 
                    Id = process.Id, 
                    Name = process.ProcessName, 
                    StartTime =  DateTime.Now, //process.StartTime,
                    Memory = process.WorkingSet64 / (1024 * 1024),
                    RespondingStatus = process.Responding,
                });
            }
            catch (Exception)
            {
                // ignored
            }
        }
        return currentProcesses.OrderBy(x => x.Name).ToList();
        
    }
    private async Task RefreshProcesses(CancellationToken cancellationToken = default)
    {
        var newProcesses = await Task.Run(GetCurrentProcesses, cancellationToken);
        Processes.Clear();
        newProcesses.ForEach(process => Processes.Add(process));
    }
    private async Task SaveToFile(CancellationToken cancellationToken = default)
    {
        try
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

            await File.WriteAllTextAsync(logFilePath, content, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    private async Task ProcessRefreshWorker(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested) 
        {
            await RefreshProcesses(cancellationToken);
            await SaveToFile(cancellationToken);
            await Task.Delay(2000, cancellationToken);
        }
    }

    private void ShowFromTray()
    {
        WindowState = WindowState.Normal;
        Show();
    }
    private void ExitApplication()
    {
        _exit = true;
        Close();
    }
    private void CreateTrayIcon()
    {
        if (_trayIcon != null) return;
        try
        {
            _trayIcon = new TaskbarIcon();
            _trayIcon.Name = "ProcessMonitorTrayIcon";
            _trayIcon.ToolTipText = "Process Monitor";
            _trayIcon.IconSource = new BitmapImage(
                new Uri("pack://application:,,,/assets/tray_icon.ico"));
            
            var contextMenu = new ContextMenu();

            // Show menu item
            var showMenuItem = new MenuItem { Header = "Show" };
            showMenuItem.Click += (s, e) => ShowFromTray();
            contextMenu.Items.Add(showMenuItem);

            contextMenu.Items.Add(new Separator());

            // Exit menu item
            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitMenuItem);

            _trayIcon.ContextMenu = contextMenu;

            // Double-click to show
            _trayIcon.TrayMouseDoubleClick += (s, e) => ShowFromTray();
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }
    }
    
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exit)
        {
            e.Cancel = true;
            Hide();
            CreateTrayIcon();
        } 
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnClosed(e);
    }
    
    private async void Refresh_OnClick(object sender, RoutedEventArgs e)
    {
        await RefreshProcesses();
        await SaveToFile();
    }

    private void StopAutoRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        if (CancellationTokenSource.Token.IsCancellationRequested) return;
        CancellationTokenSource.Cancel();
    }
    private void RunAutoRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        if (!CancellationTokenSource.Token.IsCancellationRequested) return;
        CancellationTokenSource.Dispose();
        CancellationTokenSource = new CancellationTokenSource();
        _ = ProcessRefreshWorker(CancellationTokenSource.Token);
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Console.WriteLine(TextSearch);
    }
}
