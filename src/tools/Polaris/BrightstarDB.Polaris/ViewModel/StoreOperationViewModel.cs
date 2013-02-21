using GalaSoft.MvvmLight;

namespace BrightstarDB.Polaris.ViewModel
{
    public class StoreOperationViewModel : ViewModelBase
    {
        private Connection _server;

        public Connection Server
        {
            get { return _server; }
            set
            {
                _server = value; RaisePropertyChanged("Server");
                Store = null;
            }
        }

        private Store _store;
        public Store Store
        {
            get
            {
                return _store;
            }
            set
            {
                _store = value;
                RaisePropertyChanged("Store");
            }
        }

        protected StoreOperationViewModel(Store store)
        {
            Server = store.Source;
            Store = store;
        }
    }
}
