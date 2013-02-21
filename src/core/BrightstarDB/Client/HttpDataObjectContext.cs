#if !REST_CLIENT
using System;
using System.Collections.Generic;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// This client context uses an WCF Http Client to talk with the Brightstar Server.
    /// </summary>
    public class HttpDataObjectContext : IDataObjectContext
    {
        /// <summary>
        /// The service endpoint of the brightstar WCF service.
        /// </summary>
        private readonly string _endpointUri;

        private readonly bool _optimisticLockingEnabled;

        /// <summary>
        /// Creates a new context that connects to the specific Brightstar service endpoint.
        /// </summary>
        /// <param name="connectionString">The Brightstar service connection string</param>
        public HttpDataObjectContext(ConnectionString connectionString)
        {
            _endpointUri = connectionString.ServiceEndpoint;
            _optimisticLockingEnabled = connectionString.OptimisticLocking;
        }

        /// <summary>
        /// Get the flag that indicates if optimistic locking is enabled for
        /// stores opened or created by this context by default
        /// </summary>
        public bool OptimisticLockingEnabled { get { return _optimisticLockingEnabled; } }

        /// <summary>
        /// Opens an exsting store and provides a collection of namespace mappings. 
        /// A namespace mapping looks like "bs" -> "http://www.brightstardb.com/types/"
        /// and allows uris to be passed in the form "bs:person".
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="optimisticLockingEnabled">Optional parameter to override the context's default optimistic locking setting</param>
        /// <param name="namespaceMappings">A collection of namespace mappings.</param>
        /// <returns>A IDataObjectStore instance</returns>
        public IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null)
        {
            if (!DoesStoreExist(storeName)) throw new BrightstarClientException("Store does not exist");

            if (namespaceMappings == null)
            {
                namespaceMappings = new Dictionary<string, string>();
            }

            namespaceMappings["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            namespaceMappings["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#";

            return new HttpDataObjectStore(_endpointUri, storeName, namespaceMappings, optimisticLockingEnabled.HasValue ? optimisticLockingEnabled.Value : _optimisticLockingEnabled);
        }

        /// <summary>
        /// Utility wrapper to get the WCF client.
        /// </summary>
        protected IBrightstarService Client 
        { 
            get
            {
                return BrightstarService.GetHttpClient(new Uri(_endpointUri));
            } 
        }

        /// <summary>
        /// Checks if the named store exists.
        /// </summary>
        /// <param name="storeName">The name of the store to check for</param>
        /// <returns>True if the named store exists, false otherwise</returns>
        public bool DoesStoreExist(string storeName)
        {
            return Client.DoesStoreExist(storeName);
        }

        /// <summary>
        /// Create a new store with the given name. If a store with the same name exists an exception is thrown.
        /// </summary>
        /// <param name="storeName">Name of the new store to create</param>
        /// <param name="namespaceMappings">A collection of namespace mappings.</param>
        /// <param name="optimisticLockingEnabled">Optional parameter to override the context's default optimistic locking setting</param>
        /// <param name="persistenceType">The type of persistence to use in the newly created store. If not specified, defaults to the value specified in the application configuration file or <see cref="PersistenceType.AppendOnly"/></param>
        /// <returns>A IDataObjectStore instance</returns>
        public IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null, PersistenceType? persistenceType = null)
        {
            Client.CreateStore(storeName,
                               persistenceType.HasValue ? persistenceType.Value : Configuration.PersistenceType);

            if (namespaceMappings == null)
            {
                namespaceMappings = new Dictionary<string, string>();
            }

            namespaceMappings["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            namespaceMappings["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#";

            return new HttpDataObjectStore(_endpointUri, storeName, namespaceMappings,
                                           optimisticLockingEnabled.HasValue
                                               ? optimisticLockingEnabled.Value
                                               : _optimisticLockingEnabled);
        }

        /// <summary>
        /// Deletes the named store 
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public void DeleteStore(string storeName)
        {
            Client.DeleteStore(storeName);
        }
    }
}
#endif