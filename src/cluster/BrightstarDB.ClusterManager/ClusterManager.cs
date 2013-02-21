using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.ClusterManager
{
    public class ManagerNode : IClusterManagerRequestHandler
    {
        private readonly ManagerNodeComms _comms;

        public ManagerNode(ClusterConfiguration config)
        {
            _comms = new ManagerNodeComms(config, this);
        }

        public void Start(int port)
        {
            _comms.Start(port);
        }

        public void Stop()
        {
            _comms.Stop();
        }
    }

    public interface IClusterManagerRequestHandler
    {
    }
}
