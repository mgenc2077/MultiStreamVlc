using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace MultiStreamVlc;

class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        Environment.SetEnvironmentVariable("GDK_BACKEND", "x11");
        return BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
