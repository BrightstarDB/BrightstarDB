using System.Windows.Controls;
using BrightstarDB.Polaris.ViewModel;

namespace BrightstarDB.Polaris.Views
{
    /// <summary>
    /// Interaction logic for SparqlQueryView.xaml
    /// </summary>
    public partial class SparqlQueryView : UserControl
    {
        public SparqlQueryView()
        {
            InitializeComponent();
        }

        private void TextBox_SelectionChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            var model = this.DataContext as SparqlQueryViewModel;
            if (tb != null && model != null)
            {
                model.SelectedQueryString = tb.SelectedText;
            }
        }
    }
}
