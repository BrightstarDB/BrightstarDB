using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.Xml;

namespace BrightstarDB.ClusterManager
{
    public class ClusterManagerServiceHostFactory : ServiceHostFactory
    {
        public override ServiceHostBase CreateServiceHost(string constructorString, Uri[] baseAddresses)
        {
            return CreateServiceHost();
        }

        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return CreateServiceHost();
        }

        public ServiceHost CreateServiceHost()
        {
            var clusterConfiguration = (ClusterConfiguration) ConfigurationManager.GetSection("clusterConfiguration");
            return CreateServiceHost(clusterConfiguration);
        }

        public ServiceHost CreateServiceHost(ClusterConfiguration clusterConfiguration)
        {
            var managerNode = new ManagerNode(clusterConfiguration);
            managerNode.Start();
            var serviceHost = new ServiceHost(managerNode,
                                              new[]
                                                  {
                                                      new Uri(string.Format("http://localhost:{0}/brightstarcluster",
                                                                            Configuration.HttpPort)),
                                                      new Uri(string.Format("net.tcp://localhost:{0}/brightstarcluster",
                                                                            Configuration.TcpPort)),
                                                      new Uri(string.Format("net.pipe://localhost/{0}",
                                                                            Configuration.NamedPipeName))
                                                  });

            var basicHttpBinding = new BasicHttpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netTcpContextBinding = new NetTcpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netNamedPipeBinding = new NetNamedPipeBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };

            serviceHost.AddServiceEndpoint(typeof(IBrightstarClusterManagerService), basicHttpBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarClusterManagerService), netTcpContextBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarClusterManagerService), netNamedPipeBinding, "");

            var throttlingBehavior = new ServiceThrottlingBehavior { MaxConcurrentCalls = int.MaxValue };

            serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            serviceHost.Description.Behaviors.Add(throttlingBehavior);

            serviceHost.Closed += StopNode;
            return serviceHost;
            
        }

        private void StopNode(object sender, EventArgs e)
        {
            var serviceHost = sender as ServiceHost;
            if (serviceHost != null)
            {
                var managerNode = serviceHost.SingletonInstance as ManagerNode;
                if (managerNode != null)
                {
                    managerNode.Stop();
                }
            }
        }
    }
}
