using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MultiStreamVlc;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    public ErrorDialog(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
