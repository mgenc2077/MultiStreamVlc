using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace MultiStreamVlc;

class Program
{
    [STAThread]
    public static int Main(string[] args)
        => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
