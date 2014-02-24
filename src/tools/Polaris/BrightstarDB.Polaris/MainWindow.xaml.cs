using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BrightstarDB.Client;
using BrightstarDB.Polaris.Configuration;
using BrightstarDB.Polaris.Messages;
using BrightstarDB.Polaris.ViewModel;
using BrightstarDB.Polaris.Views;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;

namespace BrightstarDB.Polaris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, HandleNewCommand));
        }

        private void HandleNewCommand(object sender, ExecutedRoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Register<DisplayToolbarMessage>(this, (action)=>HandleToolbarMessage(action));
            Messenger.Default.Register<ShowWindowMessage>(this, HandleShowWindowMessage);
            Messenger.Default.Register<ShowDialogMessage>(this, "MainWindow", HandleShowDialogMessage);
            Messenger.Default.Register<SelectTabMessage>(this, HandleSelectTabMessage);
            Messenger.Default.Register<AppExitMessage>(this, HandleAppExitMessage);
            Messenger.Default.Register<CloseTabMessage>(this, HandleCloseTabMessage);
            Messenger.Default.Register<ShowFileDialogMessage>(this, HandleShowFileDialogMessage);
            Messenger.Default.Register<AuthenticationRequiredMessage>(this, HandleAuthenticationRequiredMessage);

            if ((this.DataContext as MainViewModel).StoreSources.Count == 0)
            {
                var msg = new ShowDialogMessage(
                    Strings.NoConnectionsTitle,
                    Strings.NoConnectionsMessage,
                    MessageBoxImage.Information,
                    MessageBoxButton.YesNo,
                    (result) =>
                        {
                            if (result == MessageBoxResult.Yes)
                            {
                                var showWindowMessage = new ShowWindowMessage {Name = "NewConnection"};
                                Messenger.Default.Send(showWindowMessage);
                            }
                        });
                Messenger.Default.Send(msg, "MainWindow");
            }
        }

        private void HandleAuthenticationRequiredMessage(AuthenticationRequiredMessage msg)
        {
            var model = new CredentialsModel {PromptMessage = msg.Message};
            var credentialsDialog = new CredentialsDialog (model);
            var dialogResult = credentialsDialog.ShowDialog();
            msg.Callback(dialogResult, model.UserName, model.Password);
        }


        private void HandleCloseTabMessage(CloseTabMessage obj)
        {
            var mvm = this.DataContext as MainViewModel;
            foreach(var tb in TabItemToolbarTray.ToolBars) tb.Visibility= Visibility.Collapsed;
            mvm.TabItems.Remove(obj.TabItem);
        }

        private void HandleAppExitMessage(AppExitMessage obj)
        {
            Close();
        }

        private void HandleSelectTabMessage(SelectTabMessage msg)
        {
            TabControl.SelectedValue = msg.TabItem;
        }

        private void HandleShowDialogMessage(ShowDialogMessage msg)
        {
            var result = MessageBox.Show(this, msg.Content, msg.Title, msg.Button, msg.Icon);
            if (msg.Callback != null) msg.Callback(result);
        }

        private void HandleShowWindowMessage(ShowWindowMessage msg)
        {
            switch (msg.Name)
            {
                case "NewConnection":
                    {
                        var connectionModel = new Connection();
                        var newConnectionDialog = new ConnectionPropertiesDialog {DataContext = connectionModel};
                        var dlgResult = newConnectionDialog.ShowDialog();
                        if (dlgResult.HasValue && dlgResult.Value)
                        {
                            var model = DataContext as MainViewModel;
                            if (model != null)
                            {
                                model.StoreSources.Add(connectionModel);
                                model.Configuration.ConnectionStrings.Add(new NamedConnectionString
                                                                              {
                                                                                  Name = connectionModel.Name,
                                                                                  ConnectionString =
                                                                                      connectionModel.ConnectionString.
                                                                                      ToString()
                                                                              });
                                model.Configuration.Save();
                            }
                        }
                        break;
                    }
                case "EditConnection":
                    {
                        var connectionModel = msg.ViewModel as Connection;
                        if (connectionModel != null)
                        {
                            var editModel = connectionModel.Clone();
                            var dlg = new ConnectionPropertiesDialog {DataContext = editModel};
                            var dlgResult = dlg.ShowDialog();
                            if (dlgResult.HasValue && dlgResult.Value)
                            {
                                connectionModel.Name = editModel.Name;
                                connectionModel.ConnectionString = editModel.ConnectionString;
                                var mvm = DataContext as MainViewModel;
                                if (mvm != null)
                                {
                                    mvm.ServerRefresh(connectionModel);
                                }
                            }
                        }
                        break;
                    }
                case "PrefixesDialog":
                    {
                        var configuration = msg.ViewModel as PolarisConfigurationModel;
                        if (configuration != null)
                        {
                            var oldPrefixes = new List<PrefixConfiguration>(configuration.Prefixes);
                            var dlg = new PrefixesDialog { DataContext = configuration };
                            var dlgResult = dlg.ShowDialog();
                            if (dlgResult.HasValue && dlgResult.Value)
                            {
                                configuration.Save();
                            } else
                            {
                                configuration.Prefixes = oldPrefixes;
                            }
                        }
                        break;
                    }
                case "CreateStore":
                    {
                        var connection = msg.ViewModel as Connection;
                        if (connection != null)
                        {
                            var storeModel = new Store(connection, Guid.NewGuid().ToString());
                            var storePropertiesDialog = new StorePropertiesDialog
                                                            {DataContext = storeModel, Title = "New Store Properties"};
                            var dlgResult = storePropertiesDialog.ShowDialog();
                            if (dlgResult.HasValue && dlgResult.Value && msg.Continuation != null)
                            {
                                msg.Continuation(storePropertiesDialog.DataContext);
                            }
                        }
                        break;
                    }
                case "AboutDialog":
                    {
                        var dlg = new AboutDialog {DataContext = msg.ViewModel};
                        dlg.ShowDialog();
                        break;
                    }
            }
        }

        private static void HandleShowFileDialogMessage(ShowFileDialogMessage msg)
        {
            if (msg.IsSave)
            {
                var dlg = new SaveFileDialog
                              {
                                  FileName = msg.FileName,
                                  InitialDirectory = msg.Directory,
                                  DefaultExt = msg.DefaultExt,
                                  Filter = msg.Filter,
                                  OverwritePrompt = true,
                                  Title = msg.Title
                              };
                var result = dlg.ShowDialog();
                if (result == true)
                {
                    msg.Continuation(dlg.FileName);
                }
            }
            else
            {
                var dlg = new OpenFileDialog
                              {
                                  FileName = msg.FileName,
                                  InitialDirectory = msg.Directory,
                                  DefaultExt = msg.DefaultExt,
                                  Filter = msg.Filter,
                                  Title = msg.Title,
                                  CheckFileExists = true
                              };
                var result = dlg.ShowDialog();
                if (result == true)
                {
                    msg.Continuation(dlg.FileName);
                }
            }
        }
        private object HandleToolbarMessage(DisplayToolbarMessage action)
        {
            var toolbarTray = this.FindName("TabItemToolbarTray") as ToolBarTray;
            if (toolbarTray != null)
            {
                foreach (var tb in toolbarTray.ToolBars)
                {

                    tb.Visibility = action.Toolbars.Contains(tb.Name)
                                        ? Visibility.Visible
                                        : Visibility.Collapsed;
                }
            }
            return null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            BrightstarService.Shutdown(false);
        }

        private void HandleCloseAllTabs(object sender, RoutedEventArgs e)
        {
            foreach (var tb in TabItemToolbarTray.ToolBars) tb.Visibility = Visibility.Collapsed;
            var mvm = DataContext as MainViewModel;
            if (mvm != null)
            {
                mvm.TabItems.Clear();
            }
        }

        private void HandleNewStoreCMClick(object sender, RoutedEventArgs e)
        {
            var serverConnection = (sender as MenuItem).DataContext as Connection;
            var mvm = DataContext as MainViewModel;
            mvm.ServerCreateStore(serverConnection);
        }

        private void HandleRemoveServerCMClick(object sender, RoutedEventArgs e)
        {
            var serverConnection = (sender as MenuItem).DataContext as Connection;
            var mvm = DataContext as MainViewModel;
            mvm.ServerDisconnect(serverConnection);
        }

        private void HandleRefreshServerCMClick(object sender, RoutedEventArgs e)
        {
            var serverConnection = (sender as MenuItem).DataContext as Connection;
            var mvm = DataContext as MainViewModel;
            mvm.ServerRefresh(serverConnection);
        }

        private void HandleImportJobCMClick(object sender, RoutedEventArgs e)
        {
            var store = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.NewImportJob(store);
        }


        private void HandleQueryCMClick(object sender, RoutedEventArgs e)
        {
            var store = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.NewSparqlQuery(store);
        }

        private void HandleTransactionCMClick(object sender, RoutedEventArgs e)
        {
            var store = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.NewTransaction(store);
        }

        private void HandleHistoryCMClick(object sender, RoutedEventArgs e)
        {
            var store = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.NewHistoryView(store);
        }

        private void HandleStoreDeleteCMClick(object sender, RoutedEventArgs e)
        {
            var store = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.StoreDelete(store);
        }

        private void HandleExportJobCMClick(object sender, RoutedEventArgs e)
        {
            var store = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.NewExportJob(store);
        }

        private void HandleUpdateCMClick(object sender, RoutedEventArgs e)
        {
            var store = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.NewSparqlUpdate(store);
        }

        private void HandleEditServerCMClick(object sender, RoutedEventArgs e)
        {
            var serverConnection = (sender as MenuItem).DataContext as Connection;
            var mvm = DataContext as MainViewModel;
            mvm.ServerEdit(serverConnection);
        }

        private void HandleStatisticsCMClick(object sender, RoutedEventArgs e)
        {
            var serverConnection = (sender as MenuItem).DataContext as Store;
            var mvm = DataContext as MainViewModel;
            mvm.NewStatisticsView(serverConnection);
        }
    }
}
