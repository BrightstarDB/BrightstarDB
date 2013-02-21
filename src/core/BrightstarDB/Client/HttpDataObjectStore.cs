#if !REST_CLIENT
using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class HttpDataObjectStore : RemoteDataObjectStore
    {
        private readonly string _serviceEndpoint;

        public HttpDataObjectStore(string serviceEndpoint, string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled) : base(storeName, namespaceMappings, optimisticLockingEnabled)
        {
            _serviceEndpoint = serviceEndpoint;
        }

        protected override IBrightstarService Client
        {
            get { return BrightstarService.GetHttpClient(new Uri(_serviceEndpoint)); }
        }
    }
}
#endif