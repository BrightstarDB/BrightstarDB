#if !REST_CLIENT && !__MonoCS__
using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class HttpDataObjectStore : RemoteDataObjectStore
    {
        private readonly string _serviceEndpoint;

        public HttpDataObjectStore(string serviceEndpoint, string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled,
            string updateGraphUri = null, IEnumerable<string> datasetGraphUris = null, string versionGraphUri = null)
            : base(storeName, namespaceMappings, optimisticLockingEnabled, updateGraphUri, datasetGraphUris, versionGraphUri)
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