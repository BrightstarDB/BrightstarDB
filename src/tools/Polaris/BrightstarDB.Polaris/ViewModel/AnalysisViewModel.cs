using System.Collections.ObjectModel;
using System.Linq;
using BrightstarDB.Analysis;

namespace BrightstarDB.Polaris.ViewModel
{
    public class AnalysisViewModel
    {
        public string Label { get; set; }

        public ObservableCollection<AnalysisViewModel> Children { get; set; }

        public AnalysisViewModel()
        {
            Children = new ObservableCollection<AnalysisViewModel>();
        }
    }


    public static class AnalyserExtensions
    {
        public static long TotalKeyCount(this NodeReport root)
        {
            return root.Children.Sum(c => c.TotalKeyCount()) + root.KeyCount;
        }

        public static long TotalNodeCount(this NodeReport root)
        {
            return root.Children.Sum(c => c.TotalNodeCount()) + 1;
        }
    }
}
