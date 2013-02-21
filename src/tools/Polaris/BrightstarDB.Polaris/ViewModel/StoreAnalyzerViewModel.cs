using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Analysis;
using GalaSoft.MvvmLight;

namespace BrightstarDB.Polaris.ViewModel
{
    // KA: This class cannot be used with the new b-plus tree storage
    [Obsolete]
    public class StoreAnalyzerViewModel : ViewModelBase
    {
        public event EventHandler ReportReady;
        public event EventHandler AnalysisFailed;
        private readonly Store _store;
        private Exception _analyzerException;
        public ObservableCollection<AnalysisViewModel> Reports { get; private set; }

        public AnalysisViewModel Report
        {
            get { return Reports.FirstOrDefault(); }
            set
            {
                Reports.Clear();
                if (value != null)
                {
                    Reports.Add(value);
                    if (ReportReady != null)
                        ReportReady(this, null);
                }
            }
        }
        
        public Exception AnalyzerException
        {
            get { return _analyzerException; }
            set
            {
                _analyzerException = value;
                if (value != null)
                {
                    if (AnalysisFailed != null) AnalysisFailed(this, null);
                }
            }
        }

        
        public StoreAnalyzerViewModel(StoreReport report) 
        {
            Report = new StoreAnalysisModel(report);
        }

        public StoreAnalyzerViewModel(Store localStore) 
        {
            _store = localStore;
            Reports = new ObservableCollection<AnalysisViewModel>();
        }

        public void StartAnalysis()
        {
            var task = new Task<StoreReport>(RunAnalysis);
            task.ContinueWith(t => t != null ? (Report = new StoreAnalysisModel(t.Result)) : null,CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            task.ContinueWith(t => AnalyzerException = t.Exception,CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
            task.Start();

        }

        private StoreReport RunAnalysis()
        {
            var gatherer = new StoreDataGatherer();
            var crawler= new StoreCrawler(gatherer);
            if (_store.Source.ConnectionString.Type != ConnectionType.Embedded)
            {
                throw new InvalidOperationException("Cannot run analysis on a remote store.");
            }
            var storePath = _store.Source.ConnectionString.StoresDirectory;
            if (String.IsNullOrEmpty(storePath))
            {
                throw new InvalidOperationException("No StoresDirectory parameter found in connection string.");
            }
            crawler.Run(storePath);
            return gatherer.Report;
        }
    }
}