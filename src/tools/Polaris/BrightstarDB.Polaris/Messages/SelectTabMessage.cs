using BrightstarDB.Polaris.ViewModel;

namespace BrightstarDB.Polaris.Messages
{
    public class SelectTabMessage
    {
        public TabItemViewModel TabItem { get; private set; }
        public SelectTabMessage(TabItemViewModel tabItem)
        {
            TabItem = tabItem;
        }
    }
}
