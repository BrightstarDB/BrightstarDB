using System.Collections.Generic;
using BrightstarDB.Client;
using BrightstarDB.Storage;

namespace BrightstarDB.Azure.Gateway
{
    internal class BrightstarClusterDataObjectContext : IDataObjectContext
    {
        private IBrightstarService _client;
        private bool _optimisticLockingEnabled;

        public BrightstarClusterDataObjectContext(bool optimisticLockingEnabled)
        {
            _client = new BrightstarClusterClient();
            _optimisticLockingEnabled = optimisticLockingEnabled;
        }

        #region Implementation of IDataObjectContext

        /// <summary>
        /// Opens a new <see cref="IDataObjectStore"/> with the specified name.
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="optimisticLockingEnabled">If set to true will ensure that updates to objects modified since being read will fail.</param>
        /// <param name="namespaceMappings">A map of simple string prefixes to URIs prefixes e.g. rdfs : http://www.w3c.org/rdfs</param>
        /// <exception cref="BrightstarClientException">Thrown if the store does not exist or cannot be accessed.</exception> 
        /// <returns>IDataObjectStore</returns>
        public IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = new bool?())
        {
            if (!DoesStoreExist(storeName)) throw new BrightstarClientException("Store does not exist");
            if (namespaceMappings == null) namespaceMappings = new Dictionary<string, string>();

            namespaceMappings["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            namespaceMappings["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#";

            return new BrightstarClusterDataObjectStore( storeName, namespaceMappings,
                optimisticLockingEnabled.HasValue ? optimisticLockingEnabled.Value : _optimisticLockingEnabled);

        }

        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        public bool DoesStoreExist(string storeName)
        {
            return _client.DoesStoreExist(storeName);
        }

        /// <summary>
        /// Creates a new store.
        /// </summary>
        /// <param name="storeName">The name of the store to create</param>
        /// <param name="namespaceMappings">A collection of prefix to URI mappings to enable CURIEs to be used for resource addresses instead of full URIs</param>
        /// <param name="optimisticLockingEnabled">A boolean flag indicating if optimistic locking should be enabled</param>
        /// <returns>The newly created data object store</returns>
        public IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = new bool?(), PersistenceType? persistenceType = null)
        {
            // For now persistenceType is ignored, and the store is always an append-only store to match the way the block provider works
            _client.CreateStore(storeName);
            return new BrightstarClusterDataObjectStore(storeName, namespaceMappings,
                                                        optimisticLockingEnabled.HasValue
                                                            ? optimisticLockingEnabled.Value
                                                            : _optimisticLockingEnabled);
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public void DeleteStore(string storeName)
        {
            _client.DeleteStore(storeName);
        }

        /// <summary>
        /// Get the flag that indicates if optimistic locking is enabled for
        /// stores opened or created by this context by default
        /// </summary>
        public bool OptimisticLockingEnabled
        {
            get { return _optimisticLockingEnabled; }
        }

        #endregion
    }
}