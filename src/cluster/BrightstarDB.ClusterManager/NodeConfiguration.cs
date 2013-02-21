using System;
using System.ServiceModel;

namespace BrightstarDB.ClusterManager
{
    public class NodeConfiguration
    {
        public NodeConfiguration(){}
        public NodeConfiguration(string hostName, int managementPort, int httpPort, int tcpPort)
        {
            Host = hostName;
            ManagementPort = managementPort;
            ServiceHttpPort = httpPort;
            ServiceTcpPort = tcpPort;
        }

        public string Host { get; set; }
        public int ManagementPort { get; set; }
        public int ServiceTcpPort { get; set; }
        public int ServiceHttpPort { get; set; }

        public override int GetHashCode()
        {
            return Host.GetHashCode() ^ ManagementPort ^ ServiceTcpPort ^ ServiceHttpPort;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) throw new ArgumentNullException();
            var other = obj as NodeConfiguration;
            if (other == null) throw new ArgumentException("Expected an instance of " + GetType());
            return other.Host.Equals(Host, StringComparison.OrdinalIgnoreCase) &&
                   other.ManagementPort.Equals(ManagementPort)
                   && other.ServiceTcpPort.Equals(ServiceTcpPort)
                   && other.ServiceHttpPort.Equals(ServiceHttpPort);
        }

        public EndpointAddress GetManagementEndpoint()
        {
            return new EndpointAddress(String.Format("tcp://{0}:{1}", Host, ManagementPort));
        }

        public EndpointAddress GetServiceTcpEndpoint()
        {
            return new EndpointAddress(String.Format("tcp://{0}:{1}/brightstar", Host, ServiceTcpPort));
        }

        public EndpointAddress GetServiceHttpEndpoint()
        {
            return new EndpointAddress(String.Format("http://{0}:{1}/brightstar", Host, ServiceHttpPort));
        }
    }
}
