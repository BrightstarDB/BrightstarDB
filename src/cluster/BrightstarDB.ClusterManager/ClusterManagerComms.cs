using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterManager
{
    internal class ManagerNodeComms
    {
        private readonly ClusterConfiguration _config;
        private IClusterManagerRequestHandler _clusterManager;
        private readonly Dictionary<EndpointAddress, TcpClient> _nodeConnections;

        public ManagerNodeComms(ClusterConfiguration config, IClusterManagerRequestHandler clusterManager)
        {
            _config = config;
            _clusterManager = clusterManager;
            _nodeConnections = new Dictionary<EndpointAddress, TcpClient>();
        }

        public void Start(int port)
        {
            // TODO: Start listening
            var master = _config.ClusterEndpoints.First();
            SendMasterMessage(master);
            foreach (var slave in _config.ClusterEndpoints.Skip(1))
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

        private bool SetSlave(EndpointAddress slave, EndpointAddress master)
        {
            var socket = AssertConnection(slave);
            return SendMessageAndWaitForAck(socket, new Message("slaveof", new[]{ master.Uri.Host, master.Uri.Port.ToString("G")}));
        }

        private bool SendMasterMessage(EndpointAddress master)
        {
            var socket = AssertConnection(master);
            var msg = new Message("master", String.Empty);
            using(var writer = msg.GetContentWriter())
            {
                _config.MasterConfiguration.WriteTo(writer);
            }
            return SendMessageAndWaitForAck(socket,msg);
        }

        private TcpClient AssertConnection(EndpointAddress node)
        {
            TcpClient client;
            if (_nodeConnections.TryGetValue(node, out client)) return client;
            client = new TcpClient(node.Uri.Host, node.Uri.Port);
            _nodeConnections[node] = client;
            return client;
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
    }
}
