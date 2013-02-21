using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BrightstarDB.Polaris.Configuration;
using GalaSoft.MvvmLight.Command;
using VDS.RDF.Parsing;

namespace BrightstarDB.Polaris.ViewModel
{
    public class SparqlUpdateViewModel : StoreOperationViewModel
    {
        private bool _errorOccurred;
        private long _timeTaken;
        private string _summaryMessage;
        private string _messages;

        public SparqlUpdateViewModel(Store store, IEnumerable<PrefixConfiguration> prefixes)
            : base(store)
        {
            ExecuteCommand = new RelayCommand(ExecuteSparql);
            KeyUpCommand = new RelayCommand<KeyEventArgs>(HandleKeyUp);
            SparqlUpdateString = String.Join("\n",
                                            prefixes.Select(p => String.Format("PREFIX {0}: <{1}>", p.Prefix, p.Uri)));
            if (!String.IsNullOrEmpty(SparqlUpdateString)) SparqlUpdateString += "\n";
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

        public string SparqlUpdateString { get; set; }
        public string SummaryMessage
        {
            get { return _summaryMessage; }
            private set
            {
                _summaryMessage = value;
                RaisePropertyChanged("SummaryMessage");
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


        public RelayCommand<KeyEventArgs> KeyUpCommand { get; private set; }
        public RelayCommand ExecuteCommand { get; private set; }

        private void ExecuteSparql()
        {
            SummaryMessage = "Running update...";
            if (Store == null)
            {
                Messages = "No target store selected.";
                return;
            }
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                Store.ExecuteUpdate(SparqlUpdateString);
                timer.Stop();
                ErrorOccurred = false;
                TimeTaken = timer.ElapsedMilliseconds;
                Messages = "Update executed successfully.";
                SummaryMessage = String.Format("Update executed in {0}ms", TimeTaken);
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
        }

        private void HandleKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                ExecuteSparql();
                e.Handled = true;
            }
        }

    }
}
