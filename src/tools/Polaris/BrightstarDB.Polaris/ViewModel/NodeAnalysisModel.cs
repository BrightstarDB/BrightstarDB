using BrightstarDB.Analysis;

namespace BrightstarDB.Polaris.ViewModel
{
    public class NodeAnalysisModel : AnalysisViewModel
    {
        private readonly NodeReport _nodeReport;
        public ulong NodeId { get { return _nodeReport.NodeId; } }
        public int Depth { get { return _nodeReport.Depth; } }
        public int KeyCount { get { return _nodeReport.KeyCount; } }
        public int ChildCount { get { return _nodeReport.ChildNodeCount; } }
        public long TotalKeyCount { get; private set; }
        public long TotalChildCount { get; private set; }
        public double AvgKeysPerNode { get; private set; }

        public NodeAnalysisModel(NodeReport nodeReport)
        {
            _nodeReport = nodeReport;
            Label = "Node: " + NodeId;
            TotalKeyCount = nodeReport.KeyCount;
            TotalChildCount = nodeReport.ChildNodeCount;
            
            foreach(var rr in _nodeReport.RelatedResourceLists)
            {
                var btr = new BTreeAnalysisModel(rr);
                Children.Add(btr);
            }

            foreach(var nr in _nodeReport.Children)
            {
                var nam = new NodeAnalysisModel(nr);
                Children.Add(nam);
                TotalChildCount += nam.TotalChildCount;
                TotalKeyCount += nam.TotalKeyCount;
            }

            AvgKeysPerNode = (double) TotalKeyCount/(TotalChildCount + 1);
        }
        
    }
}