using System.Windows;

namespace BrightstarDB.Polaris.Views
{
    /// <summary>
    /// Interaction logic for PrefixesDialog.xaml
    /// </summary>
    public partial class PrefixesDialog : Window
    {
        public PrefixesDialog()
        {
            InitializeComponent();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
