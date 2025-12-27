using System;
using System.IO;
using System.Text;
using System.Windows;

namespace MultiStreamVlc
{
    public partial class App : Application
    {
        private static readonly string LogPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                         "MultiStreamVlc-crashlog.txt");

        protected override void OnStartup(StartupEventArgs e)
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

            DispatcherUnhandledException += (_, args) =>
            {
                try
                {
                    File.AppendAllText(LogPath,
                        $"=== DispatcherUnhandledException {DateTime.Now} ===\n{args.Exception}\n\n",
                        Encoding.UTF8);
                }
                catch { }
                args.Handled = false; // keep crashing so you know it failed
            };

            base.OnStartup(e);
        }
    }
}
