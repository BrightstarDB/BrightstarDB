using System.Windows;
using System.Windows.Forms;
using BrightstarDB.Polaris.Properties;
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
            var filterIndex = Settings.Default.ImportFilter;
            var fileDlg = new OpenFileDialog
                              {
                                  CheckFileExists = true,
                                  CheckPathExists = true,
                                  Filter = Strings.FileSelectorOptions,
                                  FilterIndex = filterIndex
                              };
            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                var importModel = DataContext as ImportViewModel;
                if (importModel != null)
                {
                    importModel.ImportFileName = fileDlg.FileName;
                }
                if (fileDlg.FilterIndex != filterIndex)
                {
                    Settings.Default.ImportFilter = fileDlg.FilterIndex;
                    Settings.Default.Save();
                }
            }
        }
    }
}
