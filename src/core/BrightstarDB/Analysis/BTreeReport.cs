using System.Xml.Serialization;

namespace BrightstarDB.Analysis
{
    /// <summary>
    /// Container for the results of crawling a Brightstar internal BTree index
    /// </summary>
    public class BTreeReport 
    {
        /// <summary>
        /// Get or set the index name
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }
        /// <summary>
        /// Get or set the ID of the BTree object
        /// </summary>
        [XmlAttribute]
        public ulong BtreeId { get; set; }
        /// <summary>
        /// Get or set the branching factor of the BTree
        /// </summary>
        [XmlAttribute]
        public int BranchingFactor { get; set; }
        /// <summary>
        /// Get or set the minimization factor of the BTree
        /// </summary>
        [XmlAttribute]
        public int MinimizationFactor { get; set; }
        /// <summary>
        /// Get or set the maximum depth of the BTree
        /// </summary>
        [XmlAttribute]
        public int Depth { get; set; }

        /// <summary>
        /// Get or set the report for the root node of the BTree
        /// </summary>
        [XmlElement]
        public NodeReport RootNode { get; set; }

        /// <summary>
        /// Creates an empty BTreeReport
        /// </summary>
        public BTreeReport(){}
        
        /// <summary>
        /// Creates an initialized BTreeReport
        /// </summary>
        /// <param name="name">The BTree Index Name</param>
        /// <param name="id">The BTree object ID</param>
        /// <param name="branchingFactor">The branching factor</param>
        /// <param name="minimizationFactor">The minimization factor</param>
        public BTreeReport(string name, ulong id, int branchingFactor, int minimizationFactor)
        {
            Name = name;
            BtreeId = id;
            BranchingFactor = branchingFactor;
            MinimizationFactor = minimizationFactor;
        }
    }
}