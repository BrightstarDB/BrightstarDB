using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BrightstarDB.Client;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Writing.Formatting;

namespace BrightstarDB.Polaris.ViewModel
{
    public class ImportViewModel : StoreOperationViewModel
    {
        public delegate void ImportJobDelegate();
        private Dispatcher _dispatcher;
        private string _importFileName;
        private string _progressText;
        private bool _isValid;
        private bool _isLocalImport = true;
        private IJobInfo _transactionJob;
        public static long MaxRecommendedImportSize = 50*1024*1024; // 50MB

        public ImportViewModel(Store store) : base(store)
        {
            LocalImportCheckedCommand = new RelayCommand(HandleLocalImportChecked);
            RemoteImportCheckedCommand = new RelayCommand(HandleRemoteImportChecked);
            StartClickCommand = new RelayCommand<RoutedEventArgs>(HandleStartClick);
        }

        public string ImportFileName
        {
            get { return _importFileName; }
            set
            {
                if (!_isLocalImport && !String.IsNullOrEmpty(value))
                {
                    try
                    {
                        _importFileName = Path.GetFileName(value);
                    } catch(Exception)
                    {
                        _importFileName = value;
                    }
                }
                else
                {
                    _importFileName = value;
                }
                Validate();
                RaisePropertyChanged("ImportFileName");
            }
        }

        public string ProgressText { get { return _progressText; } set { _progressText = value; RaisePropertyChanged("ProgressText"); } }
        public bool IsValid { get { return _isValid; } set { _isValid = value; RaisePropertyChanged("IsValid"); } }
        public RelayCommand LocalImportCheckedCommand { get; private set; }
        public RelayCommand RemoteImportCheckedCommand { get; private set; }
        public RelayCommand<RoutedEventArgs> StartClickCommand { get; private set; }

        private void HandleStartClick(RoutedEventArgs e)
        {
            try
            {
                var button = e.Source as Button;
                if (button != null)
                {
                    _dispatcher = button.Dispatcher;
                    if (_isLocalImport)
                    {
                        StartLocalImport();
                    }
                    else
                    {
                        StartRemoteImport();
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Log exception
                Messenger.Default.Send(
                    new ShowDialogMessage(Strings.ImportFailedDialogTitle,
                                          Strings.ImportUnexpectedErrorMsg,
                                          MessageBoxImage.Error, MessageBoxButton.OK),
                    "MainWindow");
            }
        }

        private void StartLocalImport()
        {
            var fi = new FileInfo(_importFileName);
            if (!fi.Exists)
            {
                Messenger.Default.Send(new ShowDialogMessage(
                                           Strings.ImportFailedDialogTitle,
                                           String.Format(Strings.ImportFileNotFound, ImportFileName),
                                           MessageBoxImage.Error, MessageBoxButton.OK),
                                       "MainWindow");
                return;
            }
            if (fi.Length > MaxRecommendedImportSize)
            {
                Messenger.Default.Send(new ShowDialogMessage(
                                           Strings.ImportFileSizeWarningTitle,
                                           String.Format(Strings.ImportFileSizeWarningMsg, ImportFileName),
                                           MessageBoxImage.Warning, MessageBoxButton.YesNo, LocalImportContinuation),
                                       "MainWindow");
            }
            else
            {
                LocalImportContinuation();
            }
        }

        private void LocalImportContinuation(MessageBoxResult dialogResult = MessageBoxResult.Yes)
        {
            try
            {
                if (dialogResult == MessageBoxResult.Yes)
                {
                    var ext = MimeTypesHelper.GetTrueFileExtension(_importFileName);
                    bool isGZipped = ext.EndsWith(MimeTypesHelper.DefaultGZipExtension);
                    string lines;
                    var fileTypeDefinition =
                        MimeTypesHelper.GetDefinitionsByFileExtension(ext).FirstOrDefault(d => d.CanParseRdf);
                    var rdfReader = fileTypeDefinition == null ? null : fileTypeDefinition.GetRdfParser();
                    if (rdfReader == null || rdfReader is NTriplesParser || rdfReader is NQuadsParser)
                    {
                        // We can't determine the file type, or it is NQuads or NTriples
                        if (isGZipped)
                        {
                            using (var fileStream = new FileStream(_importFileName, FileMode.Open))
                            {
                                var gZipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                                var reader = new StreamReader(gZipStream);
                                lines = reader.ReadToEnd();
                            }
                        }
                        else
                        {
                            lines = File.ReadAllText(_importFileName);
                        }
                    }
                    else
                    {
                        using (var textWriter = new StringWriter())
                        {
                            try
                            {
                                var nQuadsFormatter = new NQuadsFormatter();
                                var writeThroughHandler = new WriteThroughHandler(nQuadsFormatter, textWriter);
                                if (isGZipped)
                                {
                                    using (var fileStream = new FileStream(_importFileName, FileMode.Open))
                                    {
                                        var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                                        var streamReader = new StreamReader(gzipStream);
                                        rdfReader.Load(writeThroughHandler, streamReader);
                                    }
                                }
                                else
                                {
                                    rdfReader.Load(writeThroughHandler, _importFileName);
                                }
                                lines = textWriter.ToString();
                            }
                            catch (Exception ex)
                            {
                                Messenger.Default.Send(new ShowDialogMessage(
                                                           Strings.ParseErrorTitle,
                                                           String.Format(Strings.ParseErrorDescription, _importFileName,
                                                                         ex.Message),
                                                           MessageBoxImage.Error,
                                                           MessageBoxButton.OK),
                                                       "MainWindow");
                                return;
                            }
                        }
                    }
                    var client = BrightstarService.GetClient(Store.Source.ConnectionString);
                    _transactionJob = client.ExecuteTransaction(Store.Location, String.Empty, String.Empty, lines, false);
                    _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                                            new TransactionViewModel.JobMonitorDelegate(CheckJobStatus));
                }
            }
            catch (OutOfMemoryException)
            {
                Messenger.Default.Send(new ShowDialogMessage(Strings.ParseErrorTitle,
                                                             Strings.ImportFileTooLarge,
                                                             MessageBoxImage.Error,
                                                             MessageBoxButton.OK), "MainWindow");
            }
        }

        private void StartRemoteImport()
        {
            var client = BrightstarService.GetClient(Store.Source.ConnectionString);
            _transactionJob = client.StartImport(Store.Location, ImportFileName);
            _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                                    new TransactionViewModel.JobMonitorDelegate(CheckJobStatus));
        }

        public void CheckJobStatus()
        {
            try
            {
                var client = BrightstarService.GetClient(Store.Source.ConnectionString);
                _transactionJob = client.GetJobInfo(Store.Location, _transactionJob.JobId);
                if (_transactionJob.JobPending)
                {
                    ProgressText = "Job is currently queued for processing.";
                }
                if (_transactionJob.JobStarted)
                {
                    ProgressText = !String.IsNullOrEmpty(_transactionJob.StatusMessage)
                                       ? _transactionJob.StatusMessage
                                       : "Job is running";
                }
                if (_transactionJob.JobCompletedOk)
                {
                    ProgressText = "Job completed successfully";
                    Messenger.Default.Send(new ShowDialogMessage(
                                               Strings.ImportCompletedDialogTitle,
                                               String.Format(Strings.ImportCompletedDialogMsg, Store.Location),
                                               MessageBoxImage.Information, MessageBoxButton.OK), "MainWindow");
                }
                else if (_transactionJob.JobCompletedWithErrors)
                {
                    ProgressText = String.IsNullOrEmpty(_transactionJob.StatusMessage)
                                       ? "Import job failed. No further details provided by the server."
                                       : String.Format("Import job failed. Server reports : '{0}'",
                                                       _transactionJob.StatusMessage);
                    Messenger.Default.Send(new ShowDialogMessage(Strings.ImportFailedDialogTitle,
                                                                 String.Format(Strings.ImportFailedDialogMsg,
                                                                               Store.Location),
                                                                 MessageBoxImage.Error, MessageBoxButton.OK),
                                           "MainWindow");
                }
                else
                {
                    _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                                            new TransactionViewModel.JobMonitorDelegate(this.CheckJobStatus));
                }
            } 
            catch(Exception)
            {
                ProgressText =
                    "Error retrieving job status information from server. This may indicate a networking problem or that the server has stopped running.";
            }
        }

        private void HandleRemoteImportChecked()
        {
            _isLocalImport = false;
            // Remote file should be just the file name
            if (!String.IsNullOrEmpty(_importFileName))
            {
                try
                {
                    ImportFileName = Path.GetFileName(ImportFileName);
                }
                catch (Exception)
                {
                    ImportFileName = String.Empty;
                }
            }
        }

        private void HandleLocalImportChecked()
        {
            _isLocalImport = true;
        }

        private void Validate()
        {
            IsValid = !String.IsNullOrEmpty(_importFileName) && (!_isLocalImport || File.Exists(_importFileName));
        }

       
    }
}