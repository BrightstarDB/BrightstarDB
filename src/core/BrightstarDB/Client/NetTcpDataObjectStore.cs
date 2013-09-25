#if !REST_CLIENT && !__MonoCS__
using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class NetTcpDataObjectStore : RemoteDataObjectStore
    {
        private readonly string _serviceEndpoint;

        public NetTcpDataObjectStore(string serviceEndpoint, string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLoackingEnabled,
            string updateGraphUri, IEnumerable<string> datasetGraphUris, string versionGraphUri )
            : base(storeName, namespaceMappings, optimisticLoackingEnabled, updateGraphUri, datasetGraphUris, versionGraphUri)
        {
            _serviceEndpoint = serviceEndpoint;
        }

        protected override IBrightstarService Client
        {
            get { return BrightstarService.GetNetTcpClient(new Uri(_serviceEndpoint)); }
        }
    }
}
#endif