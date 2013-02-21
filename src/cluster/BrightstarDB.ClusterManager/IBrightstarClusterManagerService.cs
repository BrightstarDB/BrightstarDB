using System.ServiceModel;

namespace BrightstarDB.ClusterManager
{
    [ServiceContract(Namespace = "http://brightstardb.com/services/cluster/management", Name = "IBrightstarClusertManagerService")]
    interface IBrightstarClusterManagerService
    {
        /// <summary>
        /// Get the current cluster status from the cluster manager
        /// </summary>
        /// <returns>A <see cref="ClusterDescription"/> instance that describes the current cluster topology and status</returns>
        [OperationContract]
        ClusterDescription GetClusterDescription();
    }
}
