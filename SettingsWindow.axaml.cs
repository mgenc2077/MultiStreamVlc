using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace MultiStreamVlc;

public partial class SettingsWindow : Window
{
    public string Host { get; private set; } = "localhost";
    public int Port { get; private set; }
    public bool IsOk { get; private set; }

    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(string host, int port) : this()
    {
        HostText.Text = host;
        PortText.Text = port.ToString();
    }

    private void Randomize_Click(object? sender, RoutedEventArgs e)
    {
        PortText.Text = AppSettings.RandomPort().ToString();
    }

    private async void CopyPort_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(PortText.Text);
        }
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Host = HostText.Text?.Trim() ?? "localhost";
        if (string.IsNullOrWhiteSpace(Host)) Host = "localhost";

        if (!int.TryParse(PortText.Text?.Trim(), out var port) || port < 1 || port > 65535)
        {
            Port = AppSettings.RandomPort();
        }
        else
        {
            Port = port;
        }

        IsOk = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        IsOk = false;
        Close();
    }
}
