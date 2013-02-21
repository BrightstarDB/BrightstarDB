using System;
using System.Data.Services;
using System.ServiceModel;

namespace BrightstarDB.OData.Tests.Tests
{
    public class ODataServiceHostFactory 
    {
        public DataServiceHost CreateServiceHost(Uri baseUri)
        {
            var host = new DataServiceHost(typeof (ODataService), new[] {baseUri });
            host.AddServiceEndpoint(typeof (IRequestHandler), new WebHttpBinding(), "");
            return host;
        }
    }
}
