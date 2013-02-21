using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BrightstarDB.Client;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BrightstarDB.Polaris.ViewModel
{
    public class ExportViewModel : StoreOperationViewModel
    {
        public delegate void ExportJobDelegate();
        private Dispatcher _dispatcher;
        private string _exportFileName;
        private string _progressText;
        private bool _isValid;
        private IJobInfo _transactionJob;

        public ExportViewModel(Store store)
            : base(store)
        {
            StartClickCommand = new RelayCommand<RoutedEventArgs>(HandleStartClick);
            ExportFileName = String.Format(store.Location + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".nt");
        }

        public string ExportFileName
        {
            get { return _exportFileName; }
            set
            {
                _exportFileName = value;
                Validate();
                RaisePropertyChanged("ExportFileName");
            }
        }

        public string ProgressText { get { return _progressText; } set { _progressText = value; RaisePropertyChanged("ProgressText"); } }
        public bool IsValid { get { return _isValid; } set { _isValid = value; RaisePropertyChanged("IsValid"); } }
        public RelayCommand<RoutedEventArgs> StartClickCommand { get; private set; }

        private void HandleStartClick(RoutedEventArgs e)
        {
            try
            {
                var button = e.Source as Button;
                if (button != null)
                {
                    _dispatcher = button.Dispatcher;
                    StartExport();
                }
            }
            catch (Exception)
            {
                // TODO: Log exception
                Messenger.Default.Send(
                    new ShowDialogMessage(Strings.ExportFailedDialogTitle,
                                          Strings.ExportUnexpectedErrorMsg,
                                          MessageBoxImage.Error, MessageBoxButton.OK),
                    "MainWindow");
            }
        }


        private void StartExport()
        {
            var client = BrightstarService.GetClient(Store.Source.ConnectionString);
            _transactionJob = client.StartExport(Store.Location, ExportFileName);
            _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                                    new TransactionViewModel.JobMonitorDelegate(CheckJobStatus));
        }

        public void CheckJobStatus()
        {
            var client = BrightstarService.GetClient(Store.Source.ConnectionString);
            _transactionJob = client.GetJobInfo(Store.Location, _transactionJob.JobId);
            if (_transactionJob.JobPending)
            {
                ProgressText = "Job is currently queued for processing.";
            }
            if (_transactionJob.JobStarted)
            {
                ProgressText = "Job is running";
            }
            if (_transactionJob.JobCompletedOk)
            {
                ProgressText = "Job completed successfully";
                Messenger.Default.Send(new ShowDialogMessage(
                    Strings.ExportCompletedDialogTitle,
                    String.Format(Strings.ExportCompletedDialogMsg, Store.Location),
                    MessageBoxImage.Information, MessageBoxButton.OK), "MainWindow");
            }
            else if (_transactionJob.JobCompletedWithErrors)
            {
                ProgressText = String.IsNullOrEmpty(_transactionJob.StatusMessage) ?
                    "Export job failed. No further details provided by the server." :
                    String.Format("Export job failed. Server reports : '{0}'", _transactionJob.StatusMessage);
                Messenger.Default.Send(new ShowDialogMessage(Strings.ImportFailedDialogTitle,
                    String.Format(Strings.ImportFailedDialogMsg, Store.Location),
                    MessageBoxImage.Error, MessageBoxButton.OK), "MainWindow");
            }
            else
            {
                _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new TransactionViewModel.JobMonitorDelegate(this.CheckJobStatus));
            }
        }


        private void Validate()
        {
            IsValid = !String.IsNullOrEmpty(_exportFileName);
        }
    }
}
