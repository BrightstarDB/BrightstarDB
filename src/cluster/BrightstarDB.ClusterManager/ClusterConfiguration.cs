using System.Collections.Generic;
using System.ServiceModel;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterManager
{
    /// <summary>
    /// Represents the configured topology for the cluster
    /// </summary>
    public class ClusterConfiguration
    {
        /// <summary>
        /// Get or the list of nodes that form the cluster
        /// </summary>
        public List<NodeConfiguration> ClusterNodes { get; set; }

        /// <summary>
        /// Get or set the minimum number of slave nodes required for a master to enter the running state
        /// </summary>
        public MasterConfiguration MasterConfiguration { get; set; }
    }
}