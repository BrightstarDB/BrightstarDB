using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace BrightstarDB.ClusterManager
{
    [Serializable]
    public class ClusterDescription
    {
        /// <summary>
        /// Get / set the timestamp on the cluster description record
        /// </summary>
        public DateTime LastUpdated;

        public ClusterStatus Status;

        /// <summary>
        /// Get or set the address of the current cluster master node
        /// </summary>
        public Uri MasterTcpAddress;

        /// <summary>
        /// Get or set the list of addresses of the current cluster slave nodes
        /// </summary>
        public List<Uri> SlaveTcpAddresses;

        public Uri MasterHttpAddress;

        public List<Uri> SlaveHttpAddresses;
    }
}
