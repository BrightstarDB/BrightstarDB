using System.Windows;
using System.Windows.Controls;
using BrightstarDB.Polaris.ViewModel;

namespace BrightstarDB.Polaris.Views
{
    public class AnalysisDetailsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SummaryTemplate { get; set; }
        public DataTemplate BTreeTemplate { get; set; }
        public DataTemplate NodeTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is StoreAnalysisModel)
            {
                return SummaryTemplate;
            }
            if (item is BTreeAnalysisModel)
            {
                return BTreeTemplate;
            }
            if (item is NodeAnalysisModel)
            {
                return NodeTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
