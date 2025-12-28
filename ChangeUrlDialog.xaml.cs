using System.Windows;

namespace MultiStreamVlc
{
    public partial class ChangeUrlDialog : Window
    {
        public int SelectedIndex { get; private set; } = 0; // 0..5
        public string EnteredUrl { get; private set; } = "";

        public ChangeUrlDialog(int currentIndex, string currentUrl)
        {
            InitializeComponent();
            TileCombo.SelectedIndex = currentIndex;
            UrlText.Text = currentUrl ?? "";
            UrlText.SelectAll();
            UrlText.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedIndex = TileCombo.SelectedIndex;
            EnteredUrl = UrlText.Text?.Trim() ?? "";
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
