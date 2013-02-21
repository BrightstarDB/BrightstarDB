using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterManager
{
    internal class ManagerNodeComms
    {
        private readonly ClusterConfiguration _config;
        private IClusterManagerRequestHandler _clusterManager;
        private readonly Dictionary<NodeConfiguration, TcpClient> _nodeConnections;
        private NodeConfiguration _master;

        public ManagerNodeComms(ClusterConfiguration config, IClusterManagerRequestHandler clusterManager)
        {
            _config = config;
            _clusterManager = clusterManager;
            _nodeConnections = new Dictionary<NodeConfiguration, TcpClient>();
        }

        public void Start()
        {
            var master = _config.ClusterNodes.First();
            SendMasterMessage(master);
            foreach (var slave in _config.ClusterNodes.Skip(1))
            {
                try
                {
                    SetSlave(slave, master);
                }
                catch (SocketException)
                {
                    // Failed to connect to slave
                }
            }
            StartMonitoring();
        }

        private void StartMonitoring()
        {
            // Start thread to ping nodes
        }

        private bool SetSlave(NodeConfiguration slave, NodeConfiguration master)
        {
            var socket = AssertConnection(slave);
            if (socket == null) return false;
            return SendMessageAndWaitForAck(socket,
                                            new Message("slaveof",
                                                        new[] {master.Host, master.ManagementPort.ToString("G")}));
        }

        private bool SendMasterMessage(NodeConfiguration master)
        {
            var socket = AssertConnection(master);
            if (socket == null) return false;
            var msg = new Message("master", String.Empty);
            using(var writer = msg.GetContentWriter())
            {
                _config.MasterConfiguration.WriteTo(writer);
            }
            bool masterIsSet = SendMessageAndWaitForAck(socket,msg);
            if (masterIsSet)
            {
                _master = master;
            }
            return masterIsSet;
        }

        private TcpClient AssertConnection(NodeConfiguration node)
        {
            try
            {
                TcpClient client;
                if (_nodeConnections.TryGetValue(node, out client)) return client;
                client = new TcpClient(node.Host, node.ManagementPort);
                _nodeConnections[node] = client;
                return client;
            }
            catch (SocketException ex)
            {
                // Can't connect to endpoint at the moment
                return null;
            }
        }

        private bool SendMessageAndWaitForAck(TcpClient socket, Message messageToSend)
        {
            var stream = socket.GetStream();
            messageToSend.WriteTo(stream);
            stream.Flush();

            var message = Message.Read(stream);
            return message.Header.Equals("ACK", StringComparison.OrdinalIgnoreCase);
        }

        public void Stop()
        {
            // Stop the monitor thread
        }

        public ClusterDescription GetClusterDescription()
        {
            // TODO : This could be managed as a class member rather than recreated on each request
            if(_master == null)
            {
                return new ClusterDescription {LastUpdated = DateTime.UtcNow, Status = ClusterStatus.Unavailable};
            }
            var slaves = _nodeConnections.Keys.Where(x => !x.Equals(_master)).ToList();
            return new ClusterDescription
                       {
                           LastUpdated = DateTime.UtcNow,
                           MasterTcpAddress = _master.GetServiceTcpEndpoint().Uri,
                           SlaveTcpAddresses = slaves.Select(x=>x.GetServiceTcpEndpoint().Uri).ToList(),
                           MasterHttpAddress = _master.GetServiceHttpEndpoint().Uri,
                           SlaveHttpAddresses = slaves.Select(x=>x.GetServiceHttpEndpoint().Uri).ToList(),
                           Status = (slaves.Count + 1)>= _config.MasterConfiguration.WriteQuorum ? ClusterStatus.Available : ClusterStatus.ReadOnly
                       };
        }
    }
}
