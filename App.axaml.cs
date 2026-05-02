using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace MultiStreamVlc;

public partial class App : Application
{
    private static readonly string LogPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                     "MultiStreamVlc-crashlog.txt");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"=== UnhandledException {DateTime.Now} ===\n{args.ExceptionObject}\n\n",
                    Encoding.UTF8);
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"=== UnobservedTaskException {DateTime.Now} ===\n{args.Exception}\n\n",
                    Encoding.UTF8);
            }
            catch { }
            args.SetObserved();
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
