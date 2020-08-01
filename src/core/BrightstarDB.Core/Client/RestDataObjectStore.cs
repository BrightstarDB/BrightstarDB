using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class RestDataObjectStore : RemoteDataObjectStore
    {
        private readonly ConnectionString _connectionString;
        private readonly string _storeName;

        public RestDataObjectStore(ConnectionString connectionString, string storeName,
                                   Dictionary<string, string> namespaceMappings, bool isOptimisticLockingEnabled,
                                   string updateGraphUri, IEnumerable<string> datasetGraphUris, string versionGraphUri
            )
            : base(false, namespaceMappings, isOptimisticLockingEnabled,
                   updateGraphUri ?? Constants.DefaultGraphUri, datasetGraphUris, versionGraphUri)
        {
            _storeName = storeName;
            _connectionString = connectionString;
        }

        #region Overrides of RemoteDataObjectStore

        /// <summary>
        /// This must be overidden by all subclasses to create the correct client
        /// </summary>
        protected override IUpdateableStore Client
        {
            get { return new BrightstarRestUpdatableStore(BrightstarService.GetRestClient(_connectionString), _storeName); }
        }

        #endregion

        protected override void Cleanup()
        {
            // Nothing to clean up
        }
    }
}
