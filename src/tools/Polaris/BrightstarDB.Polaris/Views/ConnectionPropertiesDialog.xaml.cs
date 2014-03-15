using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using BrightstarDB.Polaris.Messages;
using BrightstarDB.Polaris.ViewModel;
using GalaSoft.MvvmLight.Messaging;

namespace BrightstarDB.Polaris.Views
{
    /// <summary>
    /// Interaction logic for ConnectionPropertiesDialog.xaml
    /// </summary>
    public partial class ConnectionPropertiesDialog : Window
    {
        public ConnectionPropertiesDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Messenger.Default.Register<CloseWindowMessage>(this, HandleCloseWindowMessage);
        }

        private void HandleCloseWindowMessage(CloseWindowMessage message)
        {
            if (message.Name.Equals("ConnectionPropertiesDialog"))
            {
                DialogResult = message.DialogResult;
                Messenger.Default.Unregister(this);
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.NewValue is Connection)
            {
                var newConnection = e.NewValue as Connection;
                newConnection.PropertyChanged += OnConnectionPropertyChanged;
                EnableFields();
            }
        }

        private void OnConnectionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("ConnectionType")) return;
            EnableFields();
        }

        private void EnableFields()
        {
            var connection = DataContext as Connection;
            if (connection == null) return;
            switch (connection.ConnectionType)
            {
                case ConnectionType.Embedded:
                    DirectoryPathText.IsEnabled = true;
                    DirectorySelectorButton.IsEnabled = true;
                    ServerEndpointText.Text = string.Empty;
                    ServerEndpointText.IsEnabled = false;
                    RequiresAuthenticationCheck.IsChecked = false;
                    RequiresAuthenticationCheck.IsEnabled = false;
                    break;
                case ConnectionType.Rest:
                    DirectoryPathText.Text = string.Empty;
                    DirectoryPathText.IsEnabled = false;
                    DirectorySelectorButton.IsEnabled = false;
                    ServerEndpointText.Text = "http://localhost:8090/brightstar";
                    ServerEndpointText.IsEnabled = true;
                    RequiresAuthenticationCheck.IsEnabled = true;
                    break;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void HandleDirectorySelectorClicked(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            dlg.Description = Strings.DirectorySelectorDescription;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryPathText.Text = dlg.SelectedPath;
            }
        }
    }
}
