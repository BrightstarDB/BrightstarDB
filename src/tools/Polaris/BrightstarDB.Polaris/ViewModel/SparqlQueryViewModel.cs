using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Polaris.Configuration;
using BrightstarDB.Polaris.Messages;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using VDS.RDF.Parsing;

namespace BrightstarDB.Polaris.ViewModel
{
    public class SparqlQueryViewModel : StoreOperationViewModel
    {
        private static readonly XNamespace SparqlResultsNs = "http://www.w3.org/2005/sparql-results#";
        private bool _errorOccurred;
        private string _messages;
        private string _resultsString;
        private DataTable _resultsTable;
        private long _rowsReturned;
        private string _summaryMessage;
        private CommitPointViewModel _targetCommitPoint;
        private long _timeTaken;

        public SparqlQueryViewModel(Store store, IEnumerable<PrefixConfiguration> prefixes)
            : base(store)
        {
            ExecuteCommand = new RelayCommand(ExecuteSparql);
            KeyUpCommand = new RelayCommand<KeyEventArgs>(HandleKeyUp);
            LoadCommand = new RelayCommand(LoadQuery);
            SaveCommand = new RelayCommand(SaveQuery);
            ResultColumnNames = new ObservableCollection<string>();
            SparqlQueryString = String.Join("\n",
                                            prefixes.Select(p => String.Format("PREFIX {0}: <{1}>", p.Prefix, p.Uri)));
            if (!String.IsNullOrEmpty(SparqlQueryString)) SparqlQueryString += "\n";
        }

        public CommitPointViewModel TargetCommitPoint
        {
            get { return _targetCommitPoint; }
            set
            {
                _targetCommitPoint = value;
                RaisePropertyChanged("TargetCommitPoint");
            }
        }

        public bool ErrorOccurred
        {
            get { return _errorOccurred; }
            set
            {
                _errorOccurred = value;
                RaisePropertyChanged("ErrorOccurred");
            }
        }

        public long TimeTaken
        {
            get { return _timeTaken; }
            set
            {
                _timeTaken = value;
                RaisePropertyChanged("TimeTaken");
            }
        }

        public long RowsReturned
        {
            get { return _rowsReturned; }
            set
            {
                _rowsReturned = value;
                RaisePropertyChanged("RowsReturned");
            }
        }

        public string SummaryMessage
        {
            get { return _summaryMessage; }
            private set
            {
                _summaryMessage = value;
                RaisePropertyChanged("SummaryMessage");
            }
        }

        public RelayCommand<KeyEventArgs> KeyUpCommand { get; private set; }

        public string SparqlQueryString { get; set; }

        public string SelectedQueryString { get; set; }

        public ObservableCollection<string> ResultColumnNames { get; private set; }

        public DataTable QueryResultsTable
        {
            get { return _resultsTable; }
            private set
            {
                _resultsTable = value;
                RaisePropertyChanged("QueryResultsTable");
            }
        }

        public String SparqlQueryResultsString
        {
            get { return _resultsString; }
            set
            {
                _resultsString = value;
                RaisePropertyChanged("SparqlQueryResultsString");
            }
        }

        public String Messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                RaisePropertyChanged("Messages");
            }
        }

        public RelayCommand ExecuteCommand { get; private set; }
        public RelayCommand LoadCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }

        private void HandleKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                ExecuteSparql();
                e.Handled = true;
            }
        }


        private void ExecuteSparql()
        {
            SummaryMessage = "Running query...";
            ResultColumnNames.Clear();
            SparqlQueryResultsString = String.Empty;
            QueryResultsTable = null;
            if (Store == null)
            {
                Messages = "No target store selected.";
                return;
            }
            ThreadPool.QueueUserWorkItem(RunSparqlQuery);
        }

        private void RunSparqlQuery(object o)
        {
            XDocument sparqlResults;
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                sparqlResults =
                    Store.ExecuteSparql(
                        String.IsNullOrEmpty(SelectedQueryString) ? SparqlQueryString : SelectedQueryString,
                        TargetCommitPoint);
                timer.Stop();
                ErrorOccurred = false;
                TimeTaken = timer.ElapsedMilliseconds;
            }
            catch (RdfParseException parseException)
            {
                SummaryMessage = "Parse error - see message tab for details.";
                Messages = "Parse error: " + parseException.Message;
                return;
            }
            catch (SparqlQueryException sqe)
            {
                SummaryMessage = "Query error - see message tab for details.";
                Messages = sqe.ToString();
                return;
            }
            catch (Exception ex)
            {
                SummaryMessage = "Query error - see message tab for details.";
                Messages = "Error running SPARQL query. " + ex.Message;
                return;
            }

            if (sparqlResults != null)
            {
                var resultsTable = new DataTable();
                SparqlQueryResultsString = sparqlResults.ToString();
                RowsReturned = sparqlResults.SparqlResultRows().Count();
                if (sparqlResults.Root != null)
                {
                    XElement sparqlQueryResults = sparqlResults.Root.Element(SparqlResultsNs + "results");
                    XElement head = sparqlResults.Root.Element(SparqlResultsNs + "head");
                    if (sparqlQueryResults != null && head != null)
                    {
                        foreach (
                            XAttribute n in
                                from v in
                                    head.Elements(SparqlResultsNs + "variable")
                                select v.Attribute("name"))
                        {
                            if (n != null)
                            {
                                ResultColumnNames.Add(n.Value);
                                resultsTable.Columns.Add(n.Value);
                            }
                        }
                    }
                    if (ResultColumnNames.Count > 0)
                    {
                        int colCount = resultsTable.Columns.Count;
                        foreach (XElement row in sparqlResults.SparqlResultRows())
                        {
                            var rowData = new object[colCount];
                            for (int i = 0; i < colCount; i++)
                            {
                                rowData[i] = row.GetColumnValue(resultsTable.Columns[i].ColumnName);
                            }
                            resultsTable.Rows.Add(rowData);
                        }
                    }
                    QueryResultsTable = resultsTable;
                }
                Messages = "Query executed successfully.";
                SummaryMessage = String.Format("Query returned {0} rows in {1}ms", RowsReturned, TimeTaken);
            }
            else
            {
                RowsReturned = 0;
                Messages = "Query executed successfully.";
                SummaryMessage = String.Format("Query executed in {0}ms", TimeTaken);
            }
        }

        private static string GetConfigurationPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var ret = Path.Combine(appDataPath, "SPARQL Queries");
            return ret;
        }

        private void LoadQuery()
        {
            Messenger.Default.Send(new ShowFileDialogMessage
                                       {
                                           Title = "Load SPARQL Query",
                                           Directory = GetConfigurationPath(),
                                           DefaultExt = ".sq",
                                           Filter = "SPARQL queries|*.sq",
                                           IsSave = false,
                                           Continuation = LoadSparqlQuery
                                       });
        }

        private void LoadSparqlQuery(string fileName)
        {
            SparqlQueryString = File.ReadAllText(fileName);
            RaisePropertyChanged("SparqlQueryString");
        }

        private void SaveQuery()
        {
            var targetPath = GetConfigurationPath();
            if (!Directory.Exists(targetPath))
            {
                Messenger.Default.Send(new ShowDialogMessage("Create Save Directory",
                                                             "We recommend saving your SPARQL Queries to a 'SPARQL Queries' folder inside your 'My Documents' folder. Such a folder does not exist at the moment. Do you want to create one now ?",
                                                             MessageBoxImage.Question, MessageBoxButton.YesNoCancel,
                                                             HandleFolderCreateDialogResult),
                                       "MainWindow");


            }
            else
            {
                Messenger.Default.Send(new ShowFileDialogMessage
                                           {
                                               Title = "Save SPARQL Query",
                                               Directory = targetPath,
                                               DefaultExt = ".sq",
                                               Filter = "SPARQL queries|*.sq",
                                               IsSave = true,
                                               Continuation = SaveSparqlQuery
                                           });
            }
        }

        private void HandleFolderCreateDialogResult(MessageBoxResult dialogResult)
        {
            if (dialogResult == MessageBoxResult.No)
            {
                Messenger.Default.Send(new ShowFileDialogMessage
                                           {
                                               Title = "Save SPARQL Query",
                                               Directory =
                                                   Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                               DefaultExt = ".sq",
                                               Filter = "SPARQL queries|*.sq",
                                               IsSave = true,
                                               Continuation = SaveSparqlQuery
                                           });
            }
            else if (dialogResult == MessageBoxResult.Yes)
            {
                var targetPath = GetConfigurationPath();
                Directory.CreateDirectory(targetPath);
                Messenger.Default.Send(new ShowFileDialogMessage
                {
                    Title = "Save SPARQL Query",
                    Directory = targetPath,
                    DefaultExt = ".sq",
                    Filter = "SPARQL queries|*.sq",
                    IsSave = true,
                    Continuation = SaveSparqlQuery
                });
            }
        }


        private void SaveSparqlQuery(string fileName)
        {
            File.WriteAllText(fileName, SparqlQueryString);
        }
    }
}