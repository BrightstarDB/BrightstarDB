using System.Windows;
using System.Windows.Controls;

namespace BrightstarDB.Polaris.Controls
{
    public class PolarisTabItem : TabItem
    {
        static PolarisTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PolarisTabItem), new FrameworkPropertyMetadata(typeof(PolarisTabItem)));
        }
    }
}
