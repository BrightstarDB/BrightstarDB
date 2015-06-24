using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BrightstarDB.Client;
using BrightstarDB.Polaris.Configuration;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BrightstarDB.Polaris.ViewModel
{
    public class TransactionViewModel : StoreOperationViewModel
    {
        private Dispatcher _dispatcher;
        private IJobInfo _transactionJob;
        private string _addTriples = String.Empty;
        public string AddTriples { get { return _addTriples; } set { _addTriples = value; RaisePropertyChanged("AddTriples"); } }

        private string _deletePatterns = String.Empty;
        public string DeletePatterns { get { return _deletePatterns; } set { _deletePatterns = value; RaisePropertyChanged("DeletePatterns"); } }

        public ObservableCollection<String> ValidationMessages { get; private set; }
        public RelayCommand<RoutedEventArgs> ExecuteCommand { get; private set; }
        private IEnumerable<PrefixConfiguration> _prefixes;

        public TransactionViewModel(Store store, IEnumerable<PrefixConfiguration> prefixes) : base(store)
        {
            ValidationMessages = new ObservableCollection<string>();
            ExecuteCommand = new RelayCommand<RoutedEventArgs>(Execute);
            _prefixes = prefixes;
        }

        private void Validate()
        {
            var triples = AddTriples.Split('\n');
            for(int i = 0; i < triples.Length; i++)
            {
                ValidateTriple(i + 1, triples[i]);
            }
            var patterns = DeletePatterns.Split('\n');
            for(int i = 0; i < patterns.Length; i++)
            {
                ValidatePattern(i + 1, patterns[i]);
            }
        }

        private void ValidateTriple(int lineNumber, string triple)
        {
            // TODO: validation for quad
        }

        private void ValidatePattern(int lineNumber, string pattern)
        {
            // TODO : validation for quad with <*> wildcards
        }

        private string GetExpandedDeletePatterns()
        {
            // Expand <*> wildcard in triples to the Brightstar wildcard match URI
            return GetExpandedTriples(DeletePatterns.Replace("<*>", "<" + Constants.WildcardUri + ">"));
        }

        private string GetExpandedTriples(string patterns)
        {
            return _prefixes.Aggregate(patterns, (current, prefix) => current.Replace("<" + prefix.Prefix + ":", "<" + prefix.Uri));
        }

        public delegate void JobMonitorDelegate();

        public void Execute(RoutedEventArgs eventArgs)
        {
            ValidationMessages.Clear();
            var executeButton = eventArgs.Source as Button;
            if (executeButton!= null)
            {
                _dispatcher = executeButton.Dispatcher;
                var client = BrightstarService.GetClient(Store.Source.ConnectionString);
                // Instead of blocking, start the job and a background thread to monitor it
                var expandedAddTriples = GetExpandedTriples(AddTriples);
                var expandedDeleteTriples = GetExpandedDeletePatterns();
                _transactionJob = client.ExecuteTransaction(Store.Location, null, expandedDeleteTriples, expandedAddTriples, waitForCompletion:false);
                ValidationMessages.Add("Executing transaction. Please wait...");
                _dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                        new JobMonitorDelegate(this.CheckJobStatus));
            }
        }

        public void CheckJobStatus()
        {
            var client = BrightstarService.GetClient(Store.Source.ConnectionString);
            _transactionJob = client.GetJobInfo(Store.Location, _transactionJob.JobId);
            if (_transactionJob.JobCompletedOk)
            {
                ValidationMessages.Clear();
                ValidationMessages.Add(Strings.TransactionSuccess);
            }
            else if (_transactionJob.JobCompletedWithErrors)
            {
                ValidationMessages.Clear();
                ValidationMessages.Add(Strings.TransactionFailed);
                ValidationMessages.Add(_transactionJob.ExtractJobErrorMessage(true));
                Messenger.Default.Send(
                    new ShowDialogMessage("Transaction failed",
                                            "Transaction failed with status: " + _transactionJob.StatusMessage,
                                            MessageBoxImage.Error, MessageBoxButton.OK), "MainWindow");
            }
            else
            {
                _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new JobMonitorDelegate(this.CheckJobStatus));
            }
        }
    }
}
