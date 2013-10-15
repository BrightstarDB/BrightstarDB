using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace BrightstarDB.Client
{
    internal class ReplyMessageInspector : IClientMessageInspector
    {
        private readonly IMessageHeaderReceiver _messageHeaderReceiver;

        public ReplyMessageInspector(IMessageHeaderReceiver messageHeaderReceiver)
        {
            _messageHeaderReceiver = messageHeaderReceiver;
        }

        #region Implementation of IClientMessageInspector

        /// <summary>
        /// Enables inspection or modification of a message before a request message is sent to a service.
        /// </summary>
        /// <returns>
        /// The object that is returned as the correlationState argument of the <see cref="M:System.ServiceModel.Dispatcher.IClientMessageInspector.AfterReceiveReply(System.ServiceModel.Channels.Message@,System.Object)"/> method. This is null if no correlation state is used.The best practice is to make this a <see cref="T:System.Guid"/> to ensure that no two correlationState objects are the same.
        /// </returns>
        /// <param name="request">The message to be sent to the service.</param><param name="channel">The  client object channel.</param>
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Enables inspection or modification of a message after a reply message is received but prior to passing it back to the client application.
        /// </summary>
        /// <param name="reply">The message to be transformed into types and handed back to the client application.</param><param name="correlationState">Correlation state data.</param>
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (_messageHeaderReceiver != null)
            {
                _messageHeaderReceiver.InspectHeaders(reply.Headers);
            }
        }

        #endregion
    }

    internal interface IMessageHeaderReceiver
    {
        void InspectHeaders(MessageHeaders messageHeaders);
    }

    internal class BrightstarServiceClientInspectorBehavior : IEndpointBehavior, IMessageHeaderReceiver
    {
        private const string SoapExtensionsNamespace = "http://brightstardb.com/schemas/service/soapExtensions#";

        private Action<DateTime?> _notifyCallback;

        internal BrightstarServiceClientInspectorBehavior(Action<DateTime?> notifyMessageHeaders)
        {
            _notifyCallback = notifyMessageHeaders;
        }

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
            
        }

        /// <summary>
        /// Implements a modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that is to be customized.</param><param name="clientRuntime">The client runtime to be customized.</param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new ReplyMessageInspector(this));
        }

        #endregion

        #region Implementation of IMessageHeaderReceiver

        public void InspectHeaders(MessageHeaders messageHeaders)
        {
            try
            {
                var timestamp = messageHeaders.GetHeader<DateTime?>("time", SoapExtensionsNamespace);
                _notifyCallback(timestamp);
            }
            catch (Exception)
            {
                _notifyCallback(null);
            }
        }

        #endregion
    }
}
