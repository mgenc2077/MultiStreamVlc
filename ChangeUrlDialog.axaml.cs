using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MultiStreamVlc;

public partial class ChangeUrlDialog : Window
{
    public int SelectedIndex { get; private set; } = 0;
    public string EnteredUrl { get; private set; } = "";
    public bool IsOk { get; private set; } = false;

    public ChangeUrlDialog()
    {
        InitializeComponent();
    }

    public ChangeUrlDialog(int currentIndex, string currentUrl) : this()
    {
        TileCombo.SelectedIndex = currentIndex;
        UrlText.Text = currentUrl ?? "";
        UrlText.SelectAll();
        UrlText.Focus();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        SelectedIndex = TileCombo.SelectedIndex;
        EnteredUrl = UrlText.Text?.Trim() ?? "";
        IsOk = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        IsOk = false;
        Close();
    }
}
