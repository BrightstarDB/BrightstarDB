using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using VDS.RDF.Parsing;

namespace BrightstarDB.Polaris.ViewModel
{
    public class Connection : ViewModelBase
    {
        private static readonly ConnectionType[] ConnectionTypesArray = new[]
                                                                            {
                                                                                ConnectionType.Embedded,
                                                                                ConnectionType.Http,
                                                                                ConnectionType.Tcp,
                                                                                ConnectionType.NamedPipe
                                                                            };

        private ConnectionString _connectionString;
        private ConnectionType _connectionType;
        private string _directoryPath;
        private bool _isValid;
        private string _name;
        private string _pipeName;
        private string _serverName;
        private string _serverPath;
        private string _serverPort;

        public Connection(string name, string connectionString)
        {
            _name = name;
            _connectionString = new ConnectionString(connectionString);
            Initialize();
        }

        public Connection()
        {
            Initialize();
        }

        public String Name
        {
            get { return _name; }
            set { _name = value; Validate(); RaisePropertyChanged("Name"); }
        }

        public IEnumerable<ConnectionType> ConnectionTypes
        {
            get { return ConnectionTypesArray; }
        }

        public bool IsValid
        {
            get { return _isValid; }
            private set { _isValid = value; RaisePropertyChanged("IsValid"); }
        }

        public ConnectionString ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                ParseConnectionString();
                RaisePropertyChanged("ConnectionString");
            }
        }

        public bool HasConnectionError { get { return !String.IsNullOrEmpty(ConnectionError); } }

        private string _connectionError;

        public string ConnectionError
        {
            get { return _connectionError; }
            private set
            {
                _connectionError = value;
                RaisePropertyChanged("ConnectionError");
                RaisePropertyChanged("HasConnectionError");
            }
        }

        public RelayCommand SaveChangesCommand { get; private set; }

        public ObservableCollection<Store> Stores { get; set; }

        public ConnectionType ConnectionType
        {
            get { return _connectionType; }
            set
            {
                _connectionType = value;
                Validate();
                RaisePropertyChanged("ConnectionType");
            }
        }

        public string ServerName
        {
            get { return _serverName; }
            set
            {
                _serverName = value;
                Validate();
                RaisePropertyChanged("ServerName");
            }
        }

        public string ServerPort
        {
            get { return _serverPort; }
            set
            {
                _serverPort = value;
                Validate();
                RaisePropertyChanged("ServerPort");
            }
        }

        public string ServerPath
        {
            get { return _serverPath; }
            set
            {
                _serverPath = value;
                Validate();
                RaisePropertyChanged("ServerPath");
            }
        }

        public string DirectoryPath
        {
            get { return _directoryPath; }
            set
            {
                _directoryPath = value;
                Validate();
                RaisePropertyChanged("DirectoryPath");
            }
        }

        public string PipeName
        {
            get { return _pipeName; }
            set
            {
                _pipeName = value;
                Validate();
                RaisePropertyChanged("PipeName");
            }
        }

        public ObservableCollection<String> ValidationMessages { get; set; }

        private void Initialize()
        {
            Stores = new ObservableCollection<Store>();
            ValidationMessages = new ObservableCollection<string>();
            SaveChangesCommand = new RelayCommand(SaveChanges);
            Validate();
        }

        private void SaveChanges()
        {
            var connString = MakeConnectionString();
            _connectionString = new ConnectionString(connString);
            TryConnect();
            Messenger.Default.Send(new CloseWindowMessage {Name = "ConnectionPropertiesDialog", DialogResult = true});
        }

        private string MakeConnectionString()
        {
            if (ConnectionType == ConnectionType.Embedded)
            {
                return String.Format("type=embedded;storesDirectory={0}", DirectoryPath);
            }
            if (ConnectionType == ConnectionType.Http || ConnectionType == ConnectionType.Tcp){
                    var connString = new StringBuilder();
                connString.Append(ConnectionType == ConnectionType.Http
                                      ? "type=http;endpoint=http://"
                                      : "type=tcp;endpoint=net.tcp://");
                connString.Append(ServerName);
                    if (!String.IsNullOrEmpty(ServerPort))
                    {
                        connString.Append(":");
                        connString.Append(ServerPort);
                    }
                if (!String.IsNullOrEmpty(ServerPath))
                {
                    if (!ServerPath.StartsWith("/"))
                    {
                        connString.Append("/");
                    }
                    connString.Append(ServerPath);
                }
                return connString.ToString();
            }
            if (ConnectionType == ConnectionType.NamedPipe)
            {
                var connString = new StringBuilder();
                connString.Append("type=namedpipe;endpoint=net.pipe://");
                connString.Append(ServerName);
                if (!PipeName.StartsWith("/"))
                {
                    connString.Append("/");
                }
                connString.Append(PipeName);
                return connString.ToString();
            }
            throw new NotSupportedException(String.Format("Cannot generate connection string for connection type {0}", ConnectionType));
        }

        private void ParseConnectionString()
        {
            ConnectionType = _connectionString.Type;
            switch (ConnectionString.Type)
            {
                case ConnectionType.Embedded:
                    ServerName = ServerPort = ServerPath = null;
                    DirectoryPath = _connectionString.StoresDirectory;
                    break;
                case ConnectionType.Http:
                case ConnectionType.Tcp:
                    var uri = new Uri(_connectionString.ServiceEndpoint);
                    ServerName = uri.Host;
                    ServerPort = uri.Port.ToString();
                    ServerPath = uri.PathAndQuery;
                    break;
                case ConnectionType.NamedPipe:
                    var npUri = new Uri(_connectionString.ServiceEndpoint);
                    ServerName = npUri.Host;
                    PipeName = npUri.PathAndQuery.TrimStart('/');
                    break;
            }
        }

        public void Validate()
        {
            ValidationMessages.Clear();
            if (String.IsNullOrEmpty(Name))
            {
                ValidationMessages.Add("A value is required for the Connection Name.");
            }
            switch (ConnectionType)
            {
                case ConnectionType.Embedded:
                    if (String.IsNullOrEmpty(DirectoryPath))
                    {
                        ValidationMessages.Add("A value is required for the Stores Directory path.");
                    } else if (!Directory.Exists(DirectoryPath))
                    {
                        ValidationMessages.Add(String.Format("Cannot find the directory {0}", DirectoryPath));
                    }
                    break;
                case ConnectionType.Http:
                case ConnectionType.Tcp:
                    if (String.IsNullOrEmpty(ServerName))
                    {
                        ValidationMessages.Add("A value is required for the Server Name.");
                    }
                    if (!String.IsNullOrEmpty(ServerPort))
                    {
                        int port;
                        if (!Int32.TryParse(ServerPort, out port) || port < 0)
                        {
                            ValidationMessages.Add("The Server Port must be a non-negative integer.");
                        }
                    }
                    break;
                case ConnectionType.NamedPipe:
                    if (String.IsNullOrEmpty(ServerName))
                    {
                        ValidationMessages.Add("A value is required for the Server Name.");
                    }
                    if (String.IsNullOrEmpty(PipeName))
                    {
                        ValidationMessages.Add("A value is required for the Pipe Name.");
                    }
                    break;
            }
            IsValid = ValidationMessages.Count == 0;
        }

        public void TryConnect()
        {
            try
            {
                Stores.Clear();
                ConnectionError = "Attempting to connect";
                var client = BrightstarService.GetClient(_connectionString);
                foreach(var storeName in  client.ListStores())
                {
                    Stores.Add(new Store(this, storeName));
                }
                ConnectionError = null;
            }
            catch (Exception)
            {
                ConnectionError = "Could not establish connection";
            }
        }

        public void CreateStore(Store store)
        {
            var client = BrightstarService.GetClient(_connectionString);
            client.CreateStore(store.Location);
            Stores.Add(store);
        }

        public void DeleteStore(string storeName)
        {
            var client = BrightstarService.GetClient(_connectionString);
            client.DeleteStore(storeName);
            foreach(var item in Stores.Where(s=>s.Location.Equals(storeName)).ToList())
            {
                Stores.Remove(item);
            }
        }

        public XDocument ExecuteQuery(Store store, string sparqlQueryString, CommitPointViewModel targetCommitPoint)
        {
            var client = BrightstarService.GetClient(_connectionString);
            try
            {
                if (targetCommitPoint == null)
                {
                    using (var resultsStream = client.ExecuteQuery(store.Location, sparqlQueryString))
                    {
                        XDocument result = XDocument.Load(resultsStream);
                        return result;
                    }
                }
                else
                {
                    var commitPoint = client.GetCommitPoint(store.Location, targetCommitPoint.CommitTime);
                    if (commitPoint == null)
                    {
                        throw new Exception("Could not retrieve specified commit point from store.");
                    }
                    using(var resultsStream = client.ExecuteQuery(commitPoint, sparqlQueryString))
                    {
                        XDocument result = XDocument.Load(resultsStream);
                        return result;
                    }
                }
            }
            catch (BrightstarClientException brightstarClientException)
            {
                ExtractSyntaxError(brightstarClientException.InnerException);
                throw new SparqlQueryException(brightstarClientException);
            }
            catch (Exception ex)
            {
                throw new SparqlQueryException(ex);
            }
        }

        public void ExecuteUpdate(Store store, string sparqlUpdateString)
        {
            try
            {
                var client = BrightstarService.GetClient(_connectionString);
                var jobInfo = client.ExecuteUpdate(store.Location, sparqlUpdateString);
                if (!jobInfo.JobCompletedOk)
                {
                    if (jobInfo.ExceptionInfo != null)
                    {
                        ExtractSyntaxError(jobInfo.ExceptionInfo);
                        throw new SparqlUpdateException(jobInfo.ExceptionInfo);
                    }
                    throw new SparqlUpdateException(jobInfo.StatusMessage);
                }
            }
            catch (Exception ex)
            {
                throw new SparqlUpdateException(ex);
            }
        }

        private static void ExtractSyntaxError(ExceptionDetail exceptionDetail)
        {
            if (exceptionDetail == null) return;
            if (exceptionDetail.Type.Equals("VDS.RDF.Parsing.RdfParseException"))
            {
                throw new RdfParseException(exceptionDetail.Message);
            }
            ExtractSyntaxError(exceptionDetail.InnerException);
        }

        public IEnumerable<CommitPointViewModel> GetCommitPoints(Store store, int skip, int take)
        {
            var client = BrightstarService.GetClient(ConnectionString);
            return client.GetCommitPoints(store.Location, skip, take).Select(x => new CommitPointViewModel(x));
        }

        public void RevertToCommitPoint(Store store, CommitPointViewModel targetCommitPoint)
        {
            if (targetCommitPoint == null) return;
            var client = BrightstarService.GetClient(ConnectionString);
            var commitPoint = client.GetCommitPoint(store.Location, targetCommitPoint.CommitTime);
            if (commitPoint == null)
            {
                throw new ApplicationException(String.Format("Could not find commit point for {0} (ID: {1})", targetCommitPoint.CommitTime, targetCommitPoint.Id));
            }
            client.RevertToCommitPoint(store.Location, commitPoint);
        }

        public IEnumerable<CommitPointViewModel> GetCommitPoints(Store store, DateTime latest, DateTime earliest, int skip, int take)
        {
            var client = BrightstarService.GetClient(ConnectionString);
            return
                client.GetCommitPoints(store.Location, latest, earliest, skip, take).Select(
                    x => new CommitPointViewModel(x));
        }

        public StatisticsViewModel GetStatistics(Store store)
        {
            var client = BrightstarService.GetClient(ConnectionString);
            var stats = client.GetStatistics(store.Location);
            return stats == null ? null : new StatisticsViewModel(stats);
        }

        public Connection Clone()
        {
            var ret = new Connection(this.Name, this.ConnectionString.ToString());
            ret.ParseConnectionString();
            return ret;
        }
    }
}