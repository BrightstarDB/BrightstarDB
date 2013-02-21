using System;
using BrightstarDB.Analysis;

namespace BrightstarDB.Polaris.ViewModel
{
    public class StoreAnalysisModel : AnalysisViewModel
    {
        private readonly StoreReport _storeReport;

        /// <summary>
        /// The path to the store directory
        /// </summary>
        public String StorePath { get { return _storeReport.StorePath; } }

        /// <summary>
        /// The date/time when this report was generated
        /// </summary>
        public DateTime ReportTimestamp { get { return _storeReport.ReportTimestamp; } }

        /// <summary>
        /// The ID of the store root object
        /// </summary>
        public ulong StoreId { get { return _storeReport.StoreId; } }

        /// <summary>
        /// The next object ID to be assigned
        /// </summary>
        public ulong NextObjectId { get { return _storeReport.NextObjectId; } }

        /// <summary>
        /// The timestamp for the last commit on the store at the time the report was generated
        /// </summary>
        public DateTime LastCommitTimestamp { get { return _storeReport.LastCommitTimestamp; } }

        /// <summary>
        /// The number of distinct predicates managed by the store
        /// </summary>
        public int PredicateCount { get { return _storeReport.PredicateCount; } }

        public StoreAnalysisModel(StoreReport storeReport)
        {
            _storeReport = storeReport;
            Label = "Store: " + LastCommitTimestamp;
            foreach (var bt in storeReport.BTrees)
            {
                Children.Add(new BTreeAnalysisModel(bt));
            }
        }
    }
}