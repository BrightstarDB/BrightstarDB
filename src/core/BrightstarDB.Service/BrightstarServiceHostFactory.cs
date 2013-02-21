using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace BrightstarDB.Service
{
    public class BrightstarServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type t, Uri[] baseAddresses)
        {
            return CreateServiceHost();
        }

        public override ServiceHostBase CreateServiceHost(string service, Uri[] baseAddresses)
        {
            return CreateServiceHost();
        }

        public ServiceHost CreateServiceHost()
        {
            var httpPort = Configuration.HttPort;
            var tcpPort = Configuration.TcpPort;
            var pipeName = Configuration.NamedPipeName;

            var serviceHost = new ServiceHost(new BrightstarService(), new[] {   new Uri(string.Format("http://localhost:{0}/brightstar", httpPort)) , 
                                                                                 new Uri(string.Format("net.tcp://localhost:{0}/brightstar", tcpPort)),
                                                                                 new Uri(string.Format("net.pipe://localhost/{0}", pipeName)) });

            var basicHttpBinding = new BasicHttpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netTcpContextBinding = new NetTcpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netNamedPipeBinding = new NetNamedPipeBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };

            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), basicHttpBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), netTcpContextBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), netNamedPipeBinding, "");

            var throttlingBehavior = new ServiceThrottlingBehavior { MaxConcurrentCalls = int.MaxValue };

            serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            serviceHost.Description.Behaviors.Add(throttlingBehavior);

            return serviceHost;
        }

        public ServiceHost CreateServiceHost(IBrightstarService service, EventHandler onCloseEventHandler)
        {
            var httpPort = Configuration.HttPort;
            var tcpPort = Configuration.TcpPort;
            var pipeName = Configuration.NamedPipeName;

            var serviceHost = new ServiceHost(service, new[] {   new Uri(string.Format("http://localhost:{0}/brightstar", httpPort)) , 
                                                                                 new Uri(string.Format("net.tcp://localhost:{0}/brightstar", tcpPort)),
                                                                                 new Uri(string.Format("net.pipe://localhost/{0}", pipeName)) });

            var basicHttpBinding = new BasicHttpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netTcpContextBinding = new NetTcpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netNamedPipeBinding = new NetNamedPipeBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };

            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), basicHttpBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), netTcpContextBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), netNamedPipeBinding, "");

            var throttlingBehavior = new ServiceThrottlingBehavior { MaxConcurrentCalls = int.MaxValue };

            serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            serviceHost.Description.Behaviors.Add(throttlingBehavior);

            serviceHost.Closed += onCloseEventHandler;

            return serviceHost;
        }


        public ServiceHost CreateServiceHost(IBrightstarService service, int httpPort, int tcpPort, string pipeName)
        {
            var serviceHost = new ServiceHost(service, new[] {   new Uri(string.Format("http://localhost:{0}/brightstar", httpPort)) , 
                                                                                 new Uri(string.Format("net.tcp://localhost:{0}/brightstar", tcpPort)),
                                                                                 new Uri(string.Format("net.pipe://localhost/{0}", pipeName)) });

            var basicHttpBinding = new BasicHttpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netTcpContextBinding = new NetTcpContextBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };
            var netNamedPipeBinding = new NetNamedPipeBinding { TransferMode = TransferMode.StreamedResponse, MaxReceivedMessageSize = int.MaxValue, SendTimeout = TimeSpan.FromMinutes(30), ReaderQuotas = XmlDictionaryReaderQuotas.Max, Namespace = "http://www.networkedplanet.com/schemas/brightstar" };

            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), basicHttpBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), netTcpContextBinding, "");
            serviceHost.AddServiceEndpoint(typeof(IBrightstarService), netNamedPipeBinding, "");

            var throttlingBehavior = new ServiceThrottlingBehavior { MaxConcurrentCalls = int.MaxValue };

            serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            serviceHost.Description.Behaviors.Add(throttlingBehavior);

            return serviceHost;
        }

    }

    internal class BrightstarServiceHeadersBehaviour : IEndpointBehavior
    {
        #region Implementation of IEndpointBehavior

        /// <summary>
        /// Implement to confirm that the endpoint meets some intended criteria.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate.</param>
        public void Validate(ServiceEndpoint endpoint)
        {

        }

        /// <summary>
        /// Implement to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="endpoint">The endpoint to modify.</param><param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {

        }

        /// <summary>
        /// Implements a modification or extension of the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param><param name="endpointDispatcher">The endpoint dispatcher to be modified or extended.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new TimestampMessageInspector());
        }

        /// <summary>
        /// Implements a modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that is to be customized.</param><param name="clientRuntime">The client runtime to be customized.</param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {

        }

        #endregion
    }

    internal class TimestampMessageInspector : IDispatchMessageInspector
    {
        private const string SoapExtensionsNamespace = "http://brightstardb.com/schemas/service/soapExtensions#";
        #region Implementation of IDispatchMessageInspector

        /// <summary>
        /// Called after an inbound message has been received but before the message is dispatched to the intended operation.
        /// </summary>
        /// <returns>
        /// The object used to correlate state. This object is passed back in the <see cref="M:System.ServiceModel.Dispatcher.IDispatchMessageInspector.BeforeSendReply(System.ServiceModel.Channels.Message@,System.Object)"/> method.
        /// </returns>
        /// <param name="request">The request message.</param><param name="channel">The incoming channel.</param><param name="instanceContext">The current service instance.</param>
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return request;
        }

        /// <summary>
        /// Called after the operation has returned but before the reply message is sent.
        /// </summary>
        /// <param name="reply">The reply message. This value is null if the operation is one way.</param><param name="correlationState">The correlation object returned from the <see cref="M:System.ServiceModel.Dispatcher.IDispatchMessageInspector.AfterReceiveRequest(System.ServiceModel.Channels.Message@,System.ServiceModel.IClientChannel,System.ServiceModel.InstanceContext)"/> method.</param>
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            reply.Headers.Add(MessageHeader.CreateHeader("time", SoapExtensionsNamespace, DateTime.UtcNow));
        }

        #endregion
    }
}