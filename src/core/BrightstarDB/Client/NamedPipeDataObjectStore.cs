#if !REST_CLIENT
using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class NamedPipeDataObjectStore : RemoteDataObjectStore
    {
        private readonly string _serviceEndpoint;

        public NamedPipeDataObjectStore(string serviceEndpoint, string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled)
            : base(storeName, namespaceMappings, optimisticLockingEnabled)
        {
            _serviceEndpoint = serviceEndpoint;
        }

        protected override IBrightstarService Client
        {
            get { return BrightstarService.GetNamedPipeClient(new Uri(_serviceEndpoint)); }
        }
    }
}
#endif