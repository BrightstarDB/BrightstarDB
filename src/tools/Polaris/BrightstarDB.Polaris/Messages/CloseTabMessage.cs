using BrightstarDB.Polaris.ViewModel;

namespace BrightstarDB.Polaris.Messages
{
    public class CloseTabMessage
    {
        public TabItemViewModel TabItem { get; private set; }
        public CloseTabMessage(TabItemViewModel tabItem)
        {
            TabItem = tabItem;
        }
    }
}