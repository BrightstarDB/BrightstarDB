using System.Windows;
using BrightstarDB.Polaris.ViewModel;

namespace BrightstarDB.Polaris.Views
{
    /// <summary>
    /// Interaction logic for CredentialsDialog.xaml
    /// </summary>
    public partial class CredentialsDialog : Window
    {
        private readonly CredentialsModel _viewModel;

        public CredentialsDialog(CredentialsModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
