#if !REST_CLIENT
using System;
using System.Collections.Generic;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// DataObjectContext that connects to BrightstarDB via a TCP connection.
    /// </summary>
    public class NetTcpDataObjectContext : IDataObjectContext
    {
        /// <summary>
        /// The service endpoint of the brightstar WCF service.
        /// </summary>
        private readonly string _endpointUri;

        private readonly bool _optimisticLockingEnabled;

        /// <summary>
        /// Get the default optimistic locking configuration for this context
        /// </summary>
        public bool OptimisticLockingEnabled { get { return _optimisticLockingEnabled; } }

        /// <summary>
        /// Creates a new context that connects to the specific Brightstar service endpoint.
        /// </summary>
        /// <param name="connectionString">The Brightstar service connection string</param>
        public NetTcpDataObjectContext(ConnectionString connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (connectionString.Type != ConnectionType.Tcp)
            {
                throw new ArgumentException("Invalid connection type", "connectionString");
            }
            _endpointUri = connectionString.ServiceEndpoint;
            _optimisticLockingEnabled = connectionString.OptimisticLocking;
        }

        /// <summary>
        /// Opens an exsting store and provides a collection of namespace mappings. 
        /// A namespace mapping looks like "bs" -> "http://www.brightstardb.com/types/"
        /// and allows uris to be passed in the form "bs:person".
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="namespaceMappings">A collection of namespace mappings.</param>
        /// <param name="optimisticLockingEnabled">Optional parameter to override the default optimistic locking setting configured on this context</param>
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

            return new NetTcpDataObjectStore(_endpointUri, storeName, namespaceMappings, optimisticLockingEnabled.HasValue ? optimisticLockingEnabled.Value : _optimisticLockingEnabled);
        }

        /// <summary>
        /// Utility wrapper to get the WCF client.
        /// </summary>
        protected IBrightstarService Client 
        { 
            get
            {
                return BrightstarService.GetNetTcpClient(new Uri(_endpointUri));
            } 
        }

        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        public bool DoesStoreExist(string storeName)
        {
            return Client.DoesStoreExist(storeName);
        }

        /// <summary>
        /// Create a new store with the given name. If a store with the same name exists an exception is thrown.
        /// </summary>
        /// <param name="storeName">Name of the new store to create</param>
        /// <param name="namespaceMappings">A collection of namespace mappings.</param>
        /// <param name="optimisticLockingEnabled">Optional parameter to override the default optimistic locking configuration for this context</param>
        /// <param name="persistenceType">The type of persistence to use in the newly created store. If not specified, defaults to the value specified in the application configuration file or <see cref="PersistenceType.AppendOnly"/></param>
        /// <returns>A IDataObjectStore instance</returns>
        public IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null, PersistenceType? persistenceType = null)
        {
            Client.CreateStore(storeName,
                persistenceType.HasValue ? persistenceType.Value : Configuration.PersistenceType);
            return new NetTcpDataObjectStore(_endpointUri, storeName, namespaceMappings, optimisticLockingEnabled.HasValue ? optimisticLockingEnabled.Value : _optimisticLockingEnabled);
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public void DeleteStore(string storeName)
        {
            Client.DeleteStore(storeName);
        }
    }
}
#endif