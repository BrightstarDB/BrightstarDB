using System;
using System.Collections.Generic;
using System.Linq;
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
        private RdfFormatViewModel _exportFileFormat;
        private string _progressText;
        private bool _isValid;
        private IJobInfo _transactionJob;

        public ExportViewModel(Store store)
            : base(store)
        {
            StartClickCommand = new RelayCommand<RoutedEventArgs>(HandleStartClick);
            ExportFileName = string.Format(store.Location + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            ExportFileFormats = RdfFormat.AllFormats.Select(x=>new RdfFormatViewModel(x)).OrderBy(x=>x.DisplayName).ToArray();
            ExportFileFormat = ExportFileFormats.FirstOrDefault(x => x.Format.DefaultExtension.Equals("nt"));
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

        public ICollection<RdfFormatViewModel> ExportFileFormats { get; }

        public RdfFormatViewModel ExportFileFormat
        {
            get { return _exportFileFormat; }
            set
            {
                var oldValue = _exportFileFormat?.Format.DefaultExtension;
                _exportFileFormat = value;
                UpdateExportFileExtension(oldValue, _exportFileFormat.Format.DefaultExtension);
                Validate();
                RaisePropertyChanged("ExportFileFormat");
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

        private void HandleExportFileFormatChanged(RoutedEventArgs e)
        {
            try
            {
                var combo = e.Source as ComboBox;
            }
            catch (Exception)
            {
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
            _transactionJob = client.StartExport(Store.Location, ExportFileName, exportFormat:ExportFileFormat.Format);
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

        private void UpdateExportFileExtension(string oldExtension, string newExtension)
        {
            if (newExtension == null) return;
            if (oldExtension == null)
            {
                ExportFileName = ExportFileName + "." + newExtension;
            }
            else
            {
                if (ExportFileName.EndsWith(oldExtension, StringComparison.OrdinalIgnoreCase))
                {
                    ExportFileName = ExportFileName.Substring(0, ExportFileName.Length - oldExtension.Length) +
                                      newExtension;
                }
            }
        }
    }
}
