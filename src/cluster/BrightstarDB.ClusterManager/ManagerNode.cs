using System.ServiceModel;

namespace BrightstarDB.ClusterManager
{
     [ServiceBehavior(Namespace = "http://brightstardb.com/services/cluster/management",
                     InstanceContextMode = InstanceContextMode.Single,
                     ConcurrencyMode = ConcurrencyMode.Multiple,
                     IncludeExceptionDetailInFaults = true)]
    public class ManagerNode : IClusterManagerRequestHandler, IBrightstarClusterManagerService
    {
        private readonly ManagerNodeComms _comms;

        public ManagerNode(ClusterConfiguration config)
        {
            _comms = new ManagerNodeComms(config, this);
        }

        public void Start()
        {
            _comms.Start();
        }

        public void Stop()
        {
            _comms.Stop();
        }

        #region Implementation of IBrightstarClusterManagerService

        /// <summary>
        /// Get the current cluster status from the cluster manager
        /// </summary>
        /// <returns>A <see cref="ClusterDescription"/> instance that describes the current cluster topology and status</returns>
        public ClusterDescription GetClusterDescription()
        {
            return _comms.GetClusterDescription();
        }

        #endregion
    }
}