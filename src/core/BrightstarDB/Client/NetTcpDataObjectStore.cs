#if !REST_CLIENT
using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class NetTcpDataObjectStore : RemoteDataObjectStore
    {
        private readonly string _serviceEndpoint;

        public NetTcpDataObjectStore(string serviceEndpoint, string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLoackingEnabled)
            : base(storeName, namespaceMappings, optimisticLoackingEnabled)
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