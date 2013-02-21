using BrightstarDB.Analysis;

namespace BrightstarDB.Polaris.ViewModel
{
    public class BTreeAnalysisModel : AnalysisViewModel
    {
        private readonly BTreeReport _btreeReport;
        /// <summary>
        /// Get or set the index name
        /// </summary>
        public string Name { get { return _btreeReport.Name; } }
        /// <summary>
        /// Get or set the ID of the BTree object
        /// </summary>
        public ulong BtreeId { get { return _btreeReport.BtreeId; } }

        /// <summary>
        /// Get or set the branching factor of the BTree
        /// </summary>
        public int BranchingFactor { get { return _btreeReport.BranchingFactor; } }

        /// <summary>
        /// Get or set the minimization factor of the BTree
        /// </summary>
        public int MinimizationFactor { get { return _btreeReport.MinimizationFactor; } }

        /// <summary>
        /// Get or set the maximum depth of the BTree
        /// </summary>
        public int Depth { get { return _btreeReport.Depth; } }

        public long TotalKeyCount { get; set; }
        public long TotalNodeCount { get; set; }
        public double AvgKeysPerNode { get; private set; }

        public BTreeAnalysisModel(BTreeReport bt)
        {
            _btreeReport = bt;
            Label = Name;
            var root = new NodeAnalysisModel(bt.RootNode);
            Children.Add(root);
            TotalKeyCount = root.TotalKeyCount;
            TotalNodeCount = root.TotalChildCount + 1;
            AvgKeysPerNode = TotalKeyCount/(double)TotalNodeCount;
        }
    }
}