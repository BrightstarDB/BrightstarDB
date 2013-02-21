using System.Collections.ObjectModel;

namespace NetworkedPlanet.Brightstar.Polaris.ViewModel
{
    public abstract class StoreSource
    {
        public abstract bool IsLocal { get; }
        public abstract string Location { get; }
        public ObservableCollection<Store> Stores { get; set; }
        protected StoreSource(){Stores = new ObservableCollection<Store>();}
    }
}