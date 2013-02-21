using System.Collections.Generic;
using BrightstarDB.Client;

namespace BrightstarDB.Azure.Gateway
{
    internal class BrightstarClusterDataObjectStore : RemoteDataObjectStore
    {
        private readonly IBrightstarService _client;
        public BrightstarClusterDataObjectStore(string storeName) : this(storeName, new Dictionary<string, string>(), false){}
        public BrightstarClusterDataObjectStore(string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled) : base(storeName, namespaceMappings, optimisticLockingEnabled)
        {
            _client = new BrightstarClusterClient();
        }

        #region Overrides of RemoteDataObjectStore

        /// <summary>
        /// This must be overidden by all subclasses to create the correct client
        /// </summary>
        protected override IBrightstarService Client
        {
            get { return _client; }
        }

        #endregion
    }
}