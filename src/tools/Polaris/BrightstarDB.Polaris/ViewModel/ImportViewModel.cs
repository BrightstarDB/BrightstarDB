using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight;
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
        private string _importGraphName;
        private string _progressText;
        private bool _isValid;
        private bool _isLocalImport = true;
        public static long MaxRecommendedImportSize = 50*1024*1024; // 50MB
        private bool _monitorStarted;

        public ImportViewModel(Store store) : base(store)
        {
            LocalImportCheckedCommand = new RelayCommand(HandleLocalImportChecked);
            RemoteImportCheckedCommand = new RelayCommand(HandleRemoteImportChecked);
            StartClickCommand = new RelayCommand<RoutedEventArgs>(HandleStartClick);
            QueuedJobs = new ObservableCollection<ImportJobViewModel>();
        }

        public string ImportFileName
        {
            get { return _importFileName; }
            set
            {
                if (!_isLocalImport && !string.IsNullOrEmpty(value))
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

        public ObservableCollection<ImportJobViewModel> QueuedJobs { get; }

        public string ImportGraphName { get { return _importGraphName; } set { _importGraphName = value; RaisePropertyChanged("ImportGraphName"); } }
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
                                rdfReader.Load(writeThroughHandler, _importFileName);
                                lines = textWriter.ToString();
                            }
                            catch (Exception ex)
                            {
                                Messenger.Default.Send(new ShowDialogMessage(
                                                           Strings.ParseErrorTitle,
                                                           string.Format(Strings.ParseErrorDescription, _importFileName,
                                                                         ex.Message),
                                                           MessageBoxImage.Error,
                                                           MessageBoxButton.OK),
                                                       "MainWindow");
                                return;
                            }
                        }
                    }
                    var client = BrightstarService.GetClient(Store.Source.ConnectionString);

                    var transactionJob = client.ExecuteTransaction(Store.Location,
                        new UpdateTransactionData {InsertData = lines, DefaultGraphUri = TargetGraphUri},
                        waitForCompletion: false);
                    QueuedJobs.Add(new ImportJobViewModel(ImportFileName, transactionJob, true));

                    if (_monitorStarted) return;
                    _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new TransactionViewModel.JobMonitorDelegate(CheckJobStatus));
                    _monitorStarted = true;
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

        private string TargetGraphUri
        {
            get
            {
                var graphUri = !string.IsNullOrWhiteSpace(ImportGraphName)
                    ? ImportGraphName
                    : Constants.DefaultGraphUri;
                return graphUri;
            }
        }

        private string JobLabel
        {
            get { return string.Format("Import of file '{0}' submitted from Polaris", ImportFileName); }
        }

        private void StartRemoteImport()
        {
            var client = BrightstarService.GetClient(Store.Source.ConnectionString);
            var importJob = client.StartImport(Store.Location, ImportFileName, TargetGraphUri, JobLabel);
            QueuedJobs.Add(new ImportJobViewModel(ImportFileName, importJob, false));
            if (!_monitorStarted)
            {
                _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                    new TransactionViewModel.JobMonitorDelegate(CheckJobStatus));
            }
        }

        public void CheckJobStatus()
        {
            try
            {
                var client = BrightstarService.GetClient(Store.Source.ConnectionString);
                var inProgress = QueuedJobs.Where(j => !j.Completed).ToList();
                if (inProgress.Any())
                {
                    foreach (var job in inProgress)
                    {
                        job.RefreshStatus(client, Store.Location);
                    }
                    Thread.Sleep(1000);
                    _dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                        new TransactionViewModel.JobMonitorDelegate(this.CheckJobStatus));
                }
                else
                {
                    _monitorStarted = false;
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

    public class ImportJobViewModel : ViewModelBase
    {
        private IJobInfo _importJobInfo;

        public string ImportFileName { get; private set; }
        public bool IsLocalImport { get; private set; }
        public string ProgressText { get; private set; }
        public string StatusImage { get; private set; }

        public ImportJobViewModel(string importFileName, IJobInfo importJobInfo, bool isLocalImport)
        {
            ImportFileName = importFileName;
            IsLocalImport = isLocalImport;
            _importJobInfo = importJobInfo;
            UpdateStatusProperties();
        }

        public void RefreshStatus(IBrightstarService client, string storeName)
        {
            _importJobInfo = client.GetJobInfo(storeName, _importJobInfo.JobId);
            UpdateStatusProperties();
        }
        private void UpdateStatusProperties()
        { 
            if (_importJobInfo.JobPending)
            {
                ProgressText = "Job is currently queued for processing.";
                StatusImage = "/Polaris;component/Resources/hourglass.png";
            }
            if (_importJobInfo.JobStarted)
            {
                ProgressText = !string.IsNullOrEmpty(_importJobInfo.StatusMessage)
                                   ? _importJobInfo.StatusMessage
                                   : "Job is running";
                StatusImage = "/Polaris;component/Resources/hourglass_go.png";
            }
            if (_importJobInfo.JobCompletedOk)
            {
                ProgressText = "Job completed successfully";
                StatusImage = "/Polaris;component/Resources/tick.png";
            }
            else if (_importJobInfo.JobCompletedWithErrors)
            {
                ProgressText = string.IsNullOrEmpty(_importJobInfo.StatusMessage)
                                   ? "Import job failed. No further details provided by the server."
                                   : $"Import job failed. Server reports : '{_importJobInfo.ExtractJobErrorMessage(stopOnFirstDetailMessage: true)}'";
                StatusImage = "/Polaris;component/Resources/exclamation.png";
            }

            RaisePropertyChanged("StatusImage");
            RaisePropertyChanged("ProgressText");
        }

        public bool Completed => _importJobInfo.JobCompletedOk || _importJobInfo.JobCompletedWithErrors;
    }
}