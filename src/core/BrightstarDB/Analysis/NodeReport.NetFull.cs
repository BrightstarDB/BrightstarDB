using System.Collections.Generic;
using System.Xml.Serialization;

namespace BrightstarDB.Analysis
{
    /// <summary>
    /// Manages the crawler information about a BTree node.
    /// </summary>
    public class NodeReport
    {
        /// <summary>
        /// Get or set the id of the node object
        /// </summary>
        [XmlAttribute]
        public ulong NodeId { get; set; }

        /// <summary>
        /// Get or set the depth at which this node is found in the tree
        /// </summary>
        [XmlAttribute]
        public int Depth { get; set; }

        /// <summary>
        /// Get or set the number of keys on this node
        /// </summary>
        [XmlAttribute]
        public int KeyCount { get; set; }

        /// <summary>
        /// Get or set the number of children of this node
        /// </summary>
        [XmlAttribute]
        public int ChildNodeCount { get; set; }

        /// <summary>
        /// The reports for the child nodes of this node
        /// </summary>
        [XmlElement(Type = typeof (NodeReport), ElementName = "Node")]
        public List<NodeReport> Children { get; set; }

        /// <summary>
        /// The reports for the related resource lists that
        /// referenced from this node
        /// </summary>
        [XmlElement(Type = typeof(BTreeReport), ElementName = "RelatedResourceList")]
        public List<BTreeReport> RelatedResourceLists { get; set; } 

        /// <summary>
        /// Creates an empty node report
        /// </summary>
        public NodeReport()
        {
            Children = new List<NodeReport>();
            RelatedResourceLists = new List<BTreeReport>();
        }

        /// <summary>
        /// Creates an initialized node report
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="currentDepth"></param>
        /// <param name="keyCount"></param>
        /// <param name="childNodeCount"></param>
        public NodeReport(ulong objectId, int currentDepth, int keyCount, int childNodeCount)
        {
            NodeId = objectId;
            Depth = currentDepth;
            KeyCount = keyCount;
            ChildNodeCount = childNodeCount;
            Children = new List<NodeReport>();
            RelatedResourceLists = new List<BTreeReport>();
        }
    }
}