using System;
using System.Windows;
using System.Windows.Controls;
using BrightstarDB.Polaris.ViewModel;

namespace BrightstarDB.Polaris.Views
{
    /// <summary>
    /// Interaction logic for StoreAnalyzerView.xaml
    /// </summary>
    public partial class StoreAnalyzerView : UserControl
    {
        public StoreAnalyzerView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = DataContext as StoreAnalyzerViewModel;
            if (vm != null)
            {
                if (vm.Report != null)
                {
                    ProgressPanel.Visibility = Visibility.Hidden;
                }
                else
                {
                    vm.ReportReady += OnReportReady;
                    vm.AnalysisFailed += OnAnalysisFailed;
                    vm.StartAnalysis();
                }
            }
        }

        private static void OnAnalysisFailed(object sender, EventArgs e)
        {
            var vm = sender as StoreAnalyzerViewModel;
            MessageBox.Show("Store analysis failed due to analyzer error:\r\n" + vm.AnalyzerException,
                            "Analysis Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnReportReady(object sender, EventArgs e)
        {
            ProgressPanel.Visibility = Visibility.Hidden;
            
        }
    }
}
