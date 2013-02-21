using System.Windows;
using System.Windows.Forms;
using BrightstarDB.Polaris.ViewModel;
using UserControl = System.Windows.Controls.UserControl;

namespace BrightstarDB.Polaris.Views
{
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class ImportView : UserControl
    {
        public ImportView()
        {
            InitializeComponent();
        }

        private void HandleFileSelectorClick(object sender, RoutedEventArgs e)
        {
            var fileDlg = new OpenFileDialog
                              {
                                  CheckFileExists = true,
                                  CheckPathExists = true,
                                  Filter = Strings.FileSelectorOptions
                              };
            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                var importModel = DataContext as ImportViewModel;
                if (importModel != null)
                {
                    importModel.ImportFileName = fileDlg.FileName;
                }
            }
        }
    }
}
