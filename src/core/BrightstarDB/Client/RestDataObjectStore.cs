using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class RestDataObjectStore : RemoteDataObjectStore
    {
        private readonly ConnectionString _connectionString;

        public RestDataObjectStore(ConnectionString connectionString, string storeName,
                                   Dictionary<string, string> namespaceMappings, bool isOptimisticLockingEnabled,
                                   string updateGraphUri, IEnumerable<string> datasetGraphUris, string versionGraphUri
            )
            : base(
                storeName, namespaceMappings, isOptimisticLockingEnabled, updateGraphUri, datasetGraphUris,
                versionGraphUri)
        {
            _connectionString = connectionString;
        }

        #region Overrides of RemoteDataObjectStore

        /// <summary>
        /// This must be overidden by all subclasses to create the correct client
        /// </summary>
        protected override IBrightstarService Client
        {
            get { return BrightstarService.GetRestClient(_connectionString); }
        }

        #endregion

        protected override void Cleanup()
        {
            // Nothing to clean up
        }
    }
}
