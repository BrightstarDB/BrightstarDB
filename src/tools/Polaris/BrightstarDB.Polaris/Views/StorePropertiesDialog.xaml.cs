using System.Windows;

namespace BrightstarDB.Polaris.Views
{
    /// <summary>
    /// Interaction logic for StorePropertiesDialog.xaml
    /// </summary>
    public partial class StorePropertiesDialog : Window
    {
        public StorePropertiesDialog()
        {
            InitializeComponent();
            Loaded += SetFocus;
        }

        private void SetFocus(object sender, RoutedEventArgs e)
        {
            StoreNameTextBox.Focus();
            StoreNameTextBox.SelectAll();
        }

        private void OnOK(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
