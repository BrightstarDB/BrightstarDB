using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using Remotion.Linq.Utilities;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;

namespace BrightstarDB.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class DotNetRdfStorageServerDataObjectContext : DotNetRdfDataObjectContextBase
    {
        private readonly IStorageServer _storageServer;
        private readonly bool _createSupported;
        private readonly bool _deleteSupported;

        /// <summary>
        /// Creates a new <see cref="IDataObjectContext"/> instance that provides access to a collection
        /// of stores managed by a DotNetRDF <see cref="IStorageServer"/> instance.
        /// </summary>
        /// <param name="storageServer">The DotNetRDF <see cref="IStorageServer"/> instance that manages the underlying data stores.</param>
        /// <param name="optimisticLockingEnabled">A boolean flag indicating if the <see cref="IDataObjectStore"/> instances managed
        /// by this context should have optimistic locking enabled by default or not.</param>
        /// <remarks>This class can be used to access servers such as Stardog and Sesame that manage a collection
        /// of triple stores.</remarks>
        public DotNetRdfStorageServerDataObjectContext(IStorageServer storageServer,
                                                       bool optimisticLockingEnabled = false)
        {
            _storageServer = storageServer;
            OptimisticLockingEnabled = optimisticLockingEnabled;
            _createSupported = ((storageServer.IOBehaviour & IOBehaviour.CanCreateStores) == IOBehaviour.CanCreateStores);
            _deleteSupported = ((storageServer.IOBehaviour & IOBehaviour.CanDeleteStores) == IOBehaviour.CanDeleteStores);
        }

        /// <summary>
        /// Opens a new <see cref="IDataObjectStore"/> with the specified name.
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="optimisticLockingEnabled">If set to true will ensure that updates to objects modified since being read will fail.</param>
        /// <param name="namespaceMappings">A map of simple string prefixes to URIs prefixes e.g. rdfs : http://www.w3c.org/rdfs</param>
        /// <param name="updateGraph">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="defaultDataSet">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve data objects and their properties.
        /// See remarks below.</param>
        /// <param name="versionTrackingGraph">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraph"/> will be used.</param>
        /// <exception cref="BrightstarClientException">Thrown if the store does not exist or cannot be accessed.</exception> 
        /// <returns>IDataObjectStore</returns>
        /// <remarks>
        /// <para>The default data set queried by context can be explicitly set by providing a value for the <paramref name="defaultDataSet"/> parameter.
        /// If an explicit data set is provided then that set of graphs, plus the update and version tracking graph will be queried by the context.
        /// If an explicit data set is not provided, but update and/or version tracking graphs are specified, then the context will query only the update and version tracking graphs.
        /// If an explicit data set is not provided and update and version tracking graphs are not specified either, then the context will query across all of the graphs in the store (and updates and version tracking
        /// information will be written to the default graph).</para>
        /// </remarks>
        public override IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null,
                                                   bool? optimisticLockingEnabled = null,
                                                   string updateGraph = null, IEnumerable<string> defaultDataSet = null,
                                                   string versionTrackingGraph = null)
        {
            IStorageProvider provider = _storageServer.GetStore(storeName);
            if (provider == null)
            {
                throw new BrightstarClientException(Strings.BrightstarServiceClient_StoreDoesNotExist);
            }
            return CreateDataObjectStore(namespaceMappings, optimisticLockingEnabled, updateGraph, defaultDataSet,
                                         versionTrackingGraph, provider);
        }

        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        /// <exception cref="BrightstarInternalException">raised if the underlying DotNetRDF storage server implementation raises an exception during this operation. 
        /// The underlying server exception will be contained in the InnerException property of the BrightstarInternalException.</exception>
        /// <exception cref="ArgumentNullException">raised if <paramref name="storeName"/> is null.</exception>
        /// <exception cref="ArgumentException">raise if <paramref name="storeName"/> is an empty string.</exception>
        public override bool DoesStoreExist(string storeName)
        {
            if (storeName == null) throw new ArgumentNullException("storeName");
            if (string.IsNullOrEmpty(storeName)) throw new ArgumentException(Strings.BrightstarServiceClient_StoreNameMustNotBeEmptyString);
            try
            {
                return _storageServer.ListStores().Contains(storeName);
            }
            catch (Exception e)
            {
             throw new BrightstarInternalException(Strings.DotNetRdf_ErrorFromUnderlyingServer, e);
            }
        }

        /// <summary>
        /// Creates a new store.
        /// </summary>
        /// <param name="storeName">The name of the store to create</param>
        /// <param name="namespaceMappings">A collection of prefix to URI mappings to enable CURIEs to be used for resource addresses instead of full URIs</param>
        /// <param name="optimisticLockingEnabled">A boolean flag indicating if optimistic locking should be enabled</param>
        /// <param name="persistenceType">The type of persistence to use in the newly created store. If not specified, defaults to the value specified in the application configuration file or <see cref="PersistenceType.AppendOnly"/></param>
        /// <param name="updateGraph">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="versionTrackingGraph">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraph"/> will be used.</param>
        /// <returns>The newly created data object store</returns>
        /// <remarks>When creating a new store through this API the default data set used for queries will be automatically set to the single update graph specified by the 
        /// <paramref name="updateGraph"/> parameter (or the default graph if the parameter is ommitted).</remarks>
        /// <exception cref="BrightstarClientException">raised if the store creation failed on the server</exception>
        /// <exception cref="BrightstarInternalException">raised if the underlying DotNetRDF storage server implementation raises an exception during this operation. 
        /// The underlying server exception will be contained in the InnerException property of the BrightstarInternalException.</exception>
        public override IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null,
                                                     bool? optimisticLockingEnabled = null,
                                                     PersistenceType? persistenceType = null,
                                                     string updateGraph = null, string versionTrackingGraph = null)
        {
            if (!_createSupported) throw new NotSupportedException(Strings.DotNetRdf_UnsupportedByServer);
            try
            {
                var storeTemplate = _storageServer.GetDefaultTemplate(storeName);
                if (_storageServer.CreateStore(storeTemplate))
                {
                    return OpenStore(storeName, namespaceMappings, optimisticLockingEnabled, updateGraph, null,
                                     versionTrackingGraph);
                }
                throw new BrightstarClientException(Strings.DotNetRdf_StoreCreationFailed);
            }
            catch (BrightstarException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BrightstarInternalException(Strings.DotNetRdf_ErrorFromUnderlyingServer, e);
            }
        }


        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        /// <exception cref="NotSupportedException">raised if the underlying DotNetRDF storage server does not support deleting the store</exception>
        /// <exception cref="BrightstarInternalException">raised if the underlying DotNetRDF storage server implementation raises an exception during this operation. 
        /// The underlying server exception will be contained in the InnerException property of the BrightstarInternalException.</exception>
        public override void DeleteStore(string storeName)
        {
            if (!_deleteSupported) throw new NotSupportedException(Strings.DotNetRdf_UnsupportedByServer);
            try
            {
                _storageServer.DeleteStore(storeName);
            }
            catch (Exception e)
            {
                throw new BrightstarInternalException(Strings.DotNetRdf_ErrorFromUnderlyingServer, e);
            }
        }

    }
}