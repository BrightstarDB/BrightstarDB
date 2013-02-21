using System;
using System.Diagnostics;
using System.Windows;
using BrightstarDB.Polaris.Configuration;
using BrightstarDB.Polaris.Messages;
using BrightstarDB.Polaris.Views;
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BrightstarDB.Polaris.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            StoreSources = new ObservableCollection<Connection>();
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                Configuration = new PolarisConfigurationModel();
                Configuration.ConnectionStrings.Add(new NamedConnectionString
                                                        {
                                                            Name = "Local",
                                                            ConnectionString =
                                                                "type=embedded;storesDirectory=c:\\brightstar"
                                                        });
                Configuration.ConnectionStrings.Add(new NamedConnectionString
                                                        {
                                                            Name="Remote",
                                                            ConnectionString = "type=http;endpoint=http://invalid/url"
                                                        });
            }
            else
            {
                // Code runs "for real"
                if (!PolarisConfigurationModel.Exists && PolarisConfigurationModel.LegacyPathExists)
                {
                    Configuration = PolarisConfigurationModel.ImportLegacyConfiguration();
                }
                else
                {
                    Configuration = PolarisConfigurationModel.Load();
                }
            }

            foreach(var namedConnection in Configuration.ConnectionStrings)
            {
                var connection = new Connection(namedConnection.Name, namedConnection.ConnectionString);
                connection.TryConnect();
                StoreSources.Add(connection);
            }
            TabItems = new ObservableCollection<TabItemViewModel>();
            NewSparqlQueryCommand = new RelayCommand<Store>((s)=>NewSparqlQuery(s));
            NewSparqlUpdateCommand = new RelayCommand<Store>((s) => NewSparqlUpdate(s));
            NewImportJobCommand = new RelayCommand<Store>(s=>NewImportJob(s));
            NewExportJobCommand = new RelayCommand<Store>(s => NewExportJob(s));
            NewTransactionCommand = new RelayCommand<Store>(s=>NewTransaction(s));
            TabItemChangedCommand = new RelayCommand<TabItemViewModel>(DisplayTabItemToolbars);
            // KA: Not currently supported for the b-plus tree store
            // AnalyzeStoreCommand = new RelayCommand<Store>(s=>AnalyzeStore(s));
            NewConnectionCommand=new RelayCommand(NewConnection);
            ServerRefreshCommand = new RelayCommand<Connection>(ServerRefresh);
            ServerEditCommand = new RelayCommand<Connection>(ServerEdit);
            ServerDisconnectCommand = new RelayCommand<Connection>(ServerDisconnect);
            ServerCreateStoreCommand = new RelayCommand<Connection>(ServerCreateStore);
            StoreDeleteCommand = new RelayCommand<Store>(StoreDelete);
            NewHistoryViewCommand = new RelayCommand<Store>(NewHistoryView);
            AboutClickCommand = new RelayCommand(About);
            ExitCommand = new RelayCommand(Exit);
            PrefixSettingsCommand = new RelayCommand(PrefixSettings);
        }

        public PolarisConfigurationModel Configuration { get; private set; }
        public ObservableCollection<Connection> StoreSources { get; set; }
        public RelayCommand NewConnectionCommand { get; private set; }
        public RelayCommand<Store> NewSparqlQueryCommand { get; private set; }
        public RelayCommand<Store> NewSparqlUpdateCommand { get; private set; }
        public RelayCommand<Store> NewTransactionCommand { get; private set; }
        public RelayCommand<Store> NewImportJobCommand { get; private set; }
        public RelayCommand<Store> NewExportJobCommand { get; private set; }
        public RelayCommand<TabItemViewModel> TabItemChangedCommand { get; private set; }
        public RelayCommand<Store> AnalyzeStoreCommand { get; private set; }
        public ObservableCollection<TabItemViewModel> TabItems { get; set; }
        public RelayCommand<Connection> ServerDisconnectCommand { get; private set; }
        public RelayCommand<Connection> ServerRefreshCommand { get; private set; }
        public RelayCommand<Connection> ServerEditCommand { get; private set; }
        public RelayCommand<Connection> ServerCreateStoreCommand { get; private set; }
        public RelayCommand<Store> StoreDeleteCommand { get; private set; }
        public RelayCommand<Store> NewHistoryViewCommand { get; private set; }
        public RelayCommand AboutClickCommand { get; private set; }
        public RelayCommand ExitCommand { get; private set; }
        public RelayCommand PrefixSettingsCommand { get; private set; }

        public object NewSparqlQuery(Store s)
        {
            var viewModel = new SparqlQueryViewModel(s, Configuration.Prefixes);
            var sparqlQueryView = new SparqlQueryView {DataContext = viewModel};
            var tabItemModel = new TabItemViewModel("SPARQL Query", sparqlQueryView);
            tabItemModel.Toolbars.Add("StoreSelectorToolbar");
            tabItemModel.Toolbars.Add("SparqlQueryToolbar");
            TabItems.Add(tabItemModel);
            Messenger.Default.Send(new SelectTabMessage(tabItemModel));
            return null;
        }

        public object NewSparqlUpdate(Store s)
        {
            var viewModel = new SparqlUpdateViewModel(s, Configuration.Prefixes);
            var sparqlUpdateView = new SparqlUpdateView {DataContext = viewModel};
            var tabItemModel = new TabItemViewModel("SPARQL Update", sparqlUpdateView);
            tabItemModel.Toolbars.Add("StoreSelectorToolbar");
            tabItemModel.Toolbars.Add("SparqlUpdateToolbar");
            TabItems.Add(tabItemModel);
            Messenger.Default.Send(new SelectTabMessage(tabItemModel));
            return null;
        }

        public void Exit()
        {
            // Notify each tab view model that the app is exiting
            // TODO: Should this be View code rather than in the model?
            foreach(var tabItemViewModel in TabItems)
            {
                if (tabItemViewModel!=null)
                {
                    if (!tabItemViewModel.HandleAppExit())
                    {
                        // tab cancelled the app exit
                        return;
                    }
                }
            }
            Messenger.Default.Send(new AppExitMessage());
        }

        public void About()
        {
            var msg = new ShowWindowMessage
                          {
                              Name = "AboutDialog",
                              ViewModel = new AboutViewModel()
                          };
            Messenger.Default.Send(msg);
        }

        public void NewConnection()
        {
            var msg = new ShowWindowMessage {Name = "NewConnection", ViewModel = this};
            Messenger.Default.Send(msg);
        }

        public void ServerDisconnect(Connection connectionToRemove)
        {
            var msg = String.Format( Strings.DisconnectServerDialogContent, connectionToRemove.Name);
            Messenger.Default.Send(
                new ShowDialogMessage(
                    Strings.DisconnectServerDialogTitle,
                    msg,
                    MessageBoxImage.Question, MessageBoxButton.YesNo,
                    result => HandleServerDisconnectDialog(connectionToRemove, result)),
                "MainWindow"
                );
        }

        private void HandleServerDisconnectDialog(Connection connectionToRemove, MessageBoxResult result)
        {
            if (result == MessageBoxResult.Yes)
            {
                Configuration.ConnectionStrings.RemoveAll(x => x.Name.Equals(connectionToRemove.Name));
                StoreSources.Remove(connectionToRemove);
                Configuration.Save();
            }
        }

        public void ServerRefresh(Connection connectionToRefresh)
        {
            connectionToRefresh.TryConnect();
        }

        public void ServerCreateStore(Connection serverConnection)
        {
            var msg = new ShowWindowMessage {Name = "CreateStore", ViewModel = serverConnection, Continuation = ContinueCreateStore};
            Messenger.Default.Send(msg);
        }

        private void ContinueCreateStore(object dataContext)
        {
            var store = dataContext as Store;
            if (store != null)
            {
                try
                {
                    store.Create();
                }
                catch (Exception ex)
                {
                    var msg = String.Format(Strings.StoreCreateFailed,
                                            String.IsNullOrEmpty(ex.Message) ? ex.GetType().ToString() : ex.Message);
                    Messenger.Default.Send(
                        new ShowDialogMessage(
                            Strings.StoreCreateFailedTitle,
                            msg,
                            MessageBoxImage.Error,
                            MessageBoxButton.OK));
                }
            }
        }


        public object NewImportJob(Store s)
        {
            var viewModel = new ImportViewModel(s);
            var importView = new ImportView {DataContext = viewModel};
            var tabItemModel = new TabItemViewModel(String.Format(Strings.ImportTabDefaultTitle, s.Location), importView);
            TabItems.Add(tabItemModel);
            Messenger.Default.Send(new SelectTabMessage(tabItemModel));
            return null;
        }

        public object NewExportJob(Store s)
        {
            var viewModel = new ExportViewModel(s);
            var exportView = new ExportView { DataContext = viewModel };
            var tabItemModel = new TabItemViewModel(String.Format(Strings.ExportTabDefaultTitle, s.Location), exportView);
            TabItems.Add(tabItemModel);
            Messenger.Default.Send(new SelectTabMessage(tabItemModel));
            return null;
        }

        public object NewTransaction(Store s)
        {
            var viewModel = new TransactionViewModel(s, Configuration.Prefixes);
            var transactionView = new TransactionView{DataContext = viewModel};
            var tabItemViewModel = new TabItemViewModel(Strings.TransactionTabDefaultTitle, transactionView);
            tabItemViewModel.Toolbars.Add("StoreSelectorToolbar");
            tabItemViewModel.Toolbars.Add("TransactionToolbar");
            TabItems.Add(tabItemViewModel);
            Messenger.Default.Send(new SelectTabMessage(tabItemViewModel));
            return null;
        }

        public void NewHistoryView(Store s)
        {
            var viewModel = new StoreHistoryViewModel(s, Configuration.Prefixes);
            var historyView = new StoreHistoryView {DataContext = viewModel};
            var tabItemViewModel = new TabItemViewModel(String.Format(Strings.HistoryTabDefaultTitle, s.Location),
                                                        historyView);
            tabItemViewModel.Toolbars.Add("SparqlQueryToolbar");
            tabItemViewModel.Toolbars.Add("HistoryToolbar");
            TabItems.Add(tabItemViewModel);
            Messenger.Default.Send(new SelectTabMessage(tabItemViewModel));
        }

        public void DisplayTabItemToolbars(TabItemViewModel tabItemViewModel)
        {
            if (tabItemViewModel == null) return;
            var msg = new DisplayToolbarMessage {Toolbars= tabItemViewModel.Toolbars};
            Messenger.Default.Send(msg);
        }
        
        // Not currently supported for the b-plus tree store
        /*
        public object AnalyzeStore(Store s)
        {
            var analyzerViewModel = new StoreAnalyzerViewModel(s);
            var analyzerView = new StoreAnalyzerView {DataContext = analyzerViewModel};
            var tabItemViewModel = new TabItemViewModel("Store Analysis", analyzerView);
            TabItems.Add(tabItemViewModel);
            Messenger.Default.Send(new SelectTabMessage(tabItemViewModel)); 
            return null;
        }
        */

        public void StoreDelete(Store storeToDelete)
        {
            try
            {
                var msg = String.Format(Strings.DeleteStoreDialogContent, storeToDelete.Location);
                Messenger.Default.Send(
                    new ShowDialogMessage(
                        Strings.DeleteStoreDialogTitle,
                        msg,
                        MessageBoxImage.Question, MessageBoxButton.YesNo,
                        result => ContinueStoreDelete(storeToDelete, result)),
                    "MainWindow"
                    );
            }
            catch (Exception ex)
            {
                HandleCommandException("Error deleting store.", ex);
            }
        }

        private void ContinueStoreDelete(Store storeToDelete, MessageBoxResult result)
        {
            try
            {
                if (result == MessageBoxResult.Yes)
                {
                    storeToDelete.Source.DeleteStore(storeToDelete.Location);
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("Error deleting store.", ex);
            }
        }

        private static void HandleCommandException(string title, Exception ex)
        {
            App.PolarisTraceSource.TraceEvent(TraceEventType.Error, 1,
                                              "{0} : {1}", title, ex);
            try
            {
                var msg = title + ": " + ex.Message;
                Messenger.Default.Send(
                    new ShowDialogMessage(title, msg, MessageBoxImage.Error, MessageBoxButton.OK), "MainWindow");
            }
            catch (Exception e)
            {
                App.PolarisTraceSource.TraceEvent(TraceEventType.Error, 1,
                                                  "Failed to display command error dialog : {0}", e);

            }
        }

        private void PrefixSettings()
        {
            var msg = new ShowWindowMessage { Name = "PrefixesDialog", ViewModel = Configuration};
            Messenger.Default.Send(msg);
        }

        public void ServerEdit(Connection serverConnection)
        {
            var msg = new ShowWindowMessage {Name = "EditConnection", ViewModel = serverConnection};
            Messenger.Default.Send(msg);
        }
    }
}