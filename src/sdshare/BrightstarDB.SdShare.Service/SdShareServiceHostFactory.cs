using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;

namespace BrightstarDB.SdShare.Service
{
    public class SdShareServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type t, Uri[] baseAddresses)
        {
            return CreateServiceHost();
        }

        public override ServiceHostBase CreateServiceHost(string service, Uri[] baseAddresses)
        {
            return CreateServiceHost();
        }

        public WebServiceHost CreateServiceHost()
        {
            var port = ConfigurationManager.AppSettings["SdShare.ServerPort"];
            var serviceHost = new WebServiceHost(new PublishingService(), new[] {   new Uri("http://localhost:" + port + "/sdshare") });            
            var webBinding = new WebHttpBinding();
            serviceHost.AddServiceEndpoint(typeof(IPublishingService), webBinding, "rest");  
         
            return serviceHost;
        }
    }
}