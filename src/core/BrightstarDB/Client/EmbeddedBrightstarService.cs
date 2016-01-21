using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
#if !PORTABLE && !WINDOWS_PHONE
#endif
using BrightstarDB.Config;
using BrightstarDB.Dto;
using BrightstarDB.Storage;
using BrightstarDB.Server;

namespace BrightstarDB.Client
{
    /// <summary>
    /// An implementation of the Brightstar Service that uses the local filestore.
    /// </summary>
    public class EmbeddedBrightstarService : IBrightstarService
    {
        private readonly ServerCore _serverCore;

        /// <summary>
        /// For an embedded service connection, this property is always null.
        /// </summary>
        public DateTime? LastResponseTimestamp { get { return null; } }


        /// <summary>
        /// Create a new instance of the service that attaches to the specified directory location
        /// </summary>
        /// <param name="baseLocation">The full path to the location of the directory that contains one or more Brightstar stores</param>
        /// <remarks>The embedded server is thread-safe but doesn't support concurrent access to the same base location by multiple
        /// instances. You should ensure in your code that only one EmbeddedBrightstarService instance is connected to any given base location
        /// at a given time. For additional control over the service internals, it is recommended to use the overload of this constructor
        /// that accepts a <see cref="EmbeddedServiceConfiguration"/></remarks>
        /// 
        public EmbeddedBrightstarService(string baseLocation)
            : this(baseLocation, Configuration.EmbeddedServiceConfiguration)
        {
            
        }
        
        /// <summary>
        /// Create a new instance of the service that attaches to the specified directory location
        /// </summary>
        /// <param name="baseLocation">The full path to the location of the directory that contains one or more Brightstar stores</param>
        /// <param name="serviceConfigurationOptions">OPTIONAL: Additional configuration options that apply only when creating an embedded service instance.</param>
        /// <remarks>The embedded server is thread-safe but doesn't support concurrent access to the same base location by multiple
        /// instances. You should ensure in your code that only one EmbeddedBrightstarService instance is connected to any given base location
        /// at a given time.</remarks>
        public EmbeddedBrightstarService(string baseLocation, EmbeddedServiceConfiguration serviceConfigurationOptions)
        {
            _serverCore = ServerCoreManager.GetServerCore(
                baseLocation,
                serviceConfigurationOptions ?? Configuration.EmbeddedServiceConfiguration);
        }

        #region Implementation of IBrightstarService

        /// <summary>
        /// List the names of the stores managed by this brightstar instance
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ListStores()
        {
            try
            {
                return _serverCore.ListStores().ToArray();
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Listing Stores");
                throw new BrightstarClientException("Error listing stores." + ex.Message, ex);
            }
        }

        /// <summary>
        /// Create a new store
        /// </summary>
        /// <param name="storeName">The name of the store to create.</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="storeName"/> is NULL</exception>
        /// <exception cref="ArgumentException">Raised if <paramref name="storeName"/> is an empty string or does not match the allowed regular expression production for store names.</exception>
        /// <remarks>See <see cref="Constants.StoreNameRegex"/> for the regular expression used to validate store names.</remarks>
        public void CreateStore(string storeName)
        {
            CreateStore(storeName, Configuration.PersistenceType);
        }

        /// <summary>
        /// Create a new store with a specific persistence type for the main indexes
        /// </summary>
        /// <param name="storeName">The name of the store to create.</param>
        /// <param name="persistenceType">The type of persistence to use for the main store indexes</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="storeName"/> is NULL</exception>
        /// <exception cref="ArgumentException">Raised if <paramref name="storeName"/> is an empty string or does not match the allowed regular expression production for store names.</exception>
        /// <remarks>See <see cref="Constants.StoreNameRegex"/> for the regular expression used to validate store names.</remarks>
        public void CreateStore(string storeName, PersistenceType persistenceType)
        {
            if (storeName == null)
                throw new ArgumentNullException("storeName", Strings.BrightstarServiceClient_StoreNameMustNotBeNull);
            if (String.IsNullOrEmpty(storeName))
                throw new ArgumentException(Strings.BrightstarServiceClient_StoreNameMustNotBeEmptyString,
                                            "storeName");
            if (!System.Text.RegularExpressions.Regex.IsMatch(storeName, Constants.StoreNameRegex))
            {
                throw new ArgumentException(Strings.BrightstarServiceClient_InvalidStoreName, "storeName");
            }
            try
            {
                _serverCore.CreateStore(storeName, persistenceType);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Creating Store {0}", storeName);
                throw new BrightstarClientException("Error creating store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to delete.</param>
        public void DeleteStore(string storeName)
        {
            try
            {
                _serverCore.DeleteStore(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Deleting Store {0}", storeName);
                throw new BrightstarClientException("Error deleting store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Checks to see if the named store already exists
        /// </summary>
        /// <param name="storeName">store to check</param>
        /// <returns>true if store exists, false if not.</returns>
        public bool DoesStoreExist(string storeName)
        {
            try
            {
                return _serverCore.DoesStoreExist(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error checking if store exists. Store {0}", storeName);
                throw new BrightstarClientException("Error checking if store exists store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// List the URIs of the named graphs contained in the specified store
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <returns>An enumeration of the URI identifiers of the named graphs in the store.</returns>
        public IEnumerable<string> ListNamedGraphs(string storeName)
        {
            if (storeName == null)
                throw new ArgumentNullException("storeName", Strings.BrightstarServiceClient_StoreNameMustNotBeNull);
            if (String.IsNullOrEmpty(storeName))
                throw new ArgumentException(Strings.BrightstarServiceClient_StoreNameMustNotBeEmptyString,
                                            "storeName");
            try
            {
                return _serverCore.ListNamedGraphs(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error listing named graphs for store {0}.", storeName);
                throw new BrightstarClientException("Error listing named graphs for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a BrightstarClientException will be raised with the message "Store not modified".</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <param name="graphFormat">OPTIONAL: Specifies the serialization format for RDF graph results. Defaults to <see cref="RdfFormat.RdfXml"/></param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression,
                                   DateTime? ifNotModifiedSince = new DateTime?(),
                                   SparqlResultsFormat resultsFormat = null,
            RdfFormat graphFormat = null)
        {
            return ExecuteQuery(storeName, queryExpression, (string[])null, ifNotModifiedSince, resultsFormat, graphFormat);
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="defaultGraphUri">The URI of the graph that will be the default graph for the query</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a BrightstarClientException will be raised with the message "Store not modified".</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <param name="graphFormat">OPTIONAL: Specifies the serialization format for RDF graph results. Defaults to <see cref="RdfFormat.RdfXml"/></param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression,
                                   string defaultGraphUri,
                                   DateTime? ifNotModifiedSince = new DateTime?(),
                                   SparqlResultsFormat resultsFormat = null,
            RdfFormat graphFormat = null)
        {
            return ExecuteQuery(storeName, queryExpression, defaultGraphUri == null ? null : new[] { defaultGraphUri }, ifNotModifiedSince,
                                resultsFormat, graphFormat);
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="defaultGraphUris">An enumeration over the URIs of the graphs that will be taken together as the default graph for the query</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a BrightstarClientException will be raised with the message "Store not modified".</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <param name="graphFormat">OPTIONAL: Specifies the serialization format for RDF graph results. Defaults to <see cref="RdfFormat.RdfXml"/></param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression, IEnumerable<string> defaultGraphUris,
                                   DateTime? ifNotModifiedSince = null,
                                   SparqlResultsFormat resultsFormat = null, RdfFormat graphFormat = null)
        {
            ISerializationFormat streamFormat;
            return ExecuteQuery(storeName, queryExpression, defaultGraphUris, ifNotModifiedSince,
                                resultsFormat ?? SparqlResultsFormat.Xml, graphFormat ?? RdfFormat.RdfXml,
                                out streamFormat);
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter has a value and the store has not been changed since the time specified,
        /// a <see cref="BrightstarStoreNotModifiedException"/> will be raised with the message "Store not modified".</param>
        /// <param name="defaultGraphUris">An enumeration over the URIs of the graphs that will be taken together as the default graph for the query. May be NULL to use the built-in default graph</param>
        /// <param name="resultsFormat">Specifies the serialization format for the SPARQL result set returned by the query. May be NULL to indicate that an RDF graph is the expected result.</param>
        /// <param name="graphFormat">Specifies the serialization format for the RDF graph returned by the query. May be NULL to indicate that a SPARQL results set is the expected result.</param>
        /// <param name="streamFormat">Specifies the serialization format used in the returned <see cref="Stream"/>.</param>
        /// <returns>A stream containing the results of executing the query</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression, IEnumerable<string> defaultGraphUris, DateTime? ifNotModifiedSince, 
            SparqlResultsFormat resultsFormat, RdfFormat graphFormat, out ISerializationFormat streamFormat)
        {
            if (storeName == null) throw new ArgumentNullException("storeName");
            if (queryExpression == null) throw new ArgumentNullException("queryExpression");
            if (resultsFormat == null && graphFormat == null) throw new ArgumentException("Either resultsFormat or graphFormat must be non-NULL");

            if (!_serverCore.DoesStoreExist(storeName)) throw new NoSuchStoreException(storeName);
            try
            {
                var pStream = new MemoryStream();
                streamFormat = _serverCore.Query(storeName, queryExpression,
                    defaultGraphUris == null ? null : defaultGraphUris.Where(x => !string.IsNullOrEmpty(x)), 
                    ifNotModifiedSince, resultsFormat,
                    graphFormat, pStream);
                return new MemoryStream(pStream.ToArray());
            }
            catch (BrightstarStoreNotModifiedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Executing Query {0} {1}", storeName, queryExpression);
                throw new BrightstarClientException("Error querying store " + storeName + " with expression " + queryExpression + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <param name="graphFormat">OPTIONAL: Specifies the serialization format for RDF graph results. Defaults to <see cref="RdfFormat.RdfXml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression,
                                   SparqlResultsFormat resultsFormat = null, RdfFormat graphFormat = null)
        {
            return ExecuteQuery(commitPoint, queryExpression, (IEnumerable<string>)null, resultsFormat, graphFormat);
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="defaultGraphUri">The URI of the default graph for the query</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <param name="graphFormat">OPTIONAL: Specifies the serialization format for RDF graph results. Defaults to <see cref="RdfFormat.RdfXml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression,
                                   string defaultGraphUri, SparqlResultsFormat resultsFormat = null, RdfFormat graphFormat = null)
        {
            return ExecuteQuery(commitPoint, queryExpression, new[] { defaultGraphUri }, resultsFormat, graphFormat);
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="defaultGraphUris">OPTIONAL: An enumeration over the URIs of the graphs that will be taken together as the default graph for the query. May be NULL to use the built-in default graph</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <param name="graphFormat">OPTIONAL: Specifies the serialization format for RDF graph results. Defaults to <see cref="RdfFormat.RdfXml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression,
                                   IEnumerable<string> defaultGraphUris,
                                   SparqlResultsFormat resultsFormat = null, RdfFormat graphFormat = null)
        {
            ISerializationFormat streamFormat;
            return ExecuteQuery(commitPoint, queryExpression, defaultGraphUris,
                                resultsFormat ?? SparqlResultsFormat.Xml, graphFormat ?? RdfFormat.RdfXml,
                                out streamFormat);
        }


        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="defaultGraphUris">An enumeration over the URIs of the graphs that will be taken together as the default graph for the query. May be NULL to use the built-in default graph</param>
        /// <param name="resultsFormat">Specifies the serialization format for the SPARQL result set returned by the query. May be NULL to indicate that an RDF graph is the expected result.</param>
        /// <param name="graphFormat">Specifies the serialization format for the RDF graph returned by the query. May be NULL to indicate that a SPARQL results set is the expected result.</param>
        /// <param name="streamFormat">Specifies the serialization format used in the returned <see cref="Stream"/>.</param>
        /// <returns>A stream containing the results of executing the query</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression, IEnumerable<string> defaultGraphUris, 
            SparqlResultsFormat resultsFormat, RdfFormat graphFormat, out ISerializationFormat streamFormat)
        {
            if (queryExpression == null) throw new ArgumentNullException("queryExpression");
            if (resultsFormat == null) resultsFormat = SparqlResultsFormat.Xml;
            if (graphFormat == null) graphFormat = RdfFormat.RdfXml;

            try
            {
                var pStream = new MemoryStream();
                streamFormat = _serverCore.Query(commitPoint.StoreName, commitPoint.Id, queryExpression, defaultGraphUris, resultsFormat, graphFormat, pStream);
                return new MemoryStream(pStream.ToArray());
            }
#if !PORTABLE && !WINDOWS_PHONE
            catch (AggregateException aggregateException)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException,
                                 "Error querying store {0}@{1} with expression {2}", commitPoint.StoreName,
                                 commitPoint.Id, queryExpression);
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    throw new BrightstarClientException(
                        String.Format("Error querying store {0}@{1} with expression {2}. {3}",
                                      commitPoint.StoreName, commitPoint.Id, queryExpression,
                                      aggregateException.InnerExceptions[0].Message));
                }
                var messages = String.Join("; ", aggregateException.InnerExceptions.Select(ex => ex.Message));
                throw new BrightstarClientException(
                    String.Format(
                        "Error querying store {0}@{1} with expression {2}. Multiple errors occurred: {3}",
                        commitPoint.StoreName, commitPoint.Id, queryExpression, messages));
            }
#endif
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException,
                                 "Error querying store {0}@{1} with expression {2}", commitPoint.StoreName,
                                 commitPoint.Id, queryExpression);
                throw new BrightstarClientException(
                    String.Format("Error querying store {0}@{1} with expression {2}. {3}", commitPoint.StoreName,
                                  commitPoint.Id, queryExpression, ex.Message), ex);
            }
        }

#if PORTABLE
        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions">NTriples that must be in the store in order for the transaction to execute</param>
        /// <param name="deletePatterns">The delete patterns that will be removed from the store</param>
        /// <param name="insertData">The NTriples data that will be inserted into the store.</param>
        /// <param name="defaultGraphUri">The URI of the default graph to apply the transaction to.</param>
        /// <returns>Job Info</returns>
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns,
                                           string insertData, string defaultGraphUri, string label = null)
        {
            try
            {
                var jobId = _serverCore.ProcessTransaction(storeName, preconditions, String.Empty,
                                                           deletePatterns, insertData,
                                                           defaultGraphUri, label);
                return GetJobInfo(storeName, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Queing Transaction {0} {1} {2}", storeName, deletePatterns, insertData);
                throw new BrightstarClientException("Error queing transaction in store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="updateTransaction">The update transaction data</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="JobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteTransaction(string storeName, UpdateTransactionData updateTransaction,
                                           string label = null)
        {
            try
            {
                var jobId = _serverCore.ProcessTransaction(storeName,
                                                           updateTransaction.ExistencePreconditions,
                                                           updateTransaction.NonexistencePreconditions,
                                                           updateTransaction.DeletePatterns,
                                                           updateTransaction.InsertData,
                                                           updateTransaction.DefaultGraphUri,
                                                           label);
                return GetJobInfo(storeName, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Queing Transaction {0} {1} {2}",
                                 storeName, updateTransaction.DeletePatterns, updateTransaction.InsertData);
                throw new BrightstarClientException("Error queing transaction in store " + storeName + ". " + ex.Message, ex);
            }
        }
#else
        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions">NTriples that must be in the store in order for the transaction to execute</param>
        /// <param name="deletePatterns">The delete patterns that will be removed from the store</param>
        /// <param name="insertData">The NTriples data that will be inserted into the store.</param>
        /// <param name="defaultGraphUri">The URI of the default graph to apply the transaction to.</param>
        /// <param name="waitForCompletion">If set to true the method will block until the transaction completes</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>Job Info</returns>
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns,
                                           string insertData,
                                           string defaultGraphUri, bool waitForCompletion = true, string label = null)
        {
            return ExecuteTransaction(storeName,
                                      new UpdateTransactionData
                                          {
                                              ExistencePreconditions = preconditions,
                                              NonexistencePreconditions = String.Empty,
                                              DeletePatterns = deletePatterns,
                                              InsertData = insertData,
                                              DefaultGraphUri = defaultGraphUri
                                          },
                                      waitForCompletion, label);
        }


        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="updateTransaction">The update transaction data</param>
        /// <param name="waitForCompletion">If set to true the method will block until the transaction completes</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteTransaction(string storeName, UpdateTransactionData updateTransaction,
                                           bool waitForCompletion = true, string label = null)
        {
            try
            {
                if (!waitForCompletion)
                {
                    var jobId = _serverCore.ProcessTransaction(storeName, updateTransaction.ExistencePreconditions,
                                                               updateTransaction.NonexistencePreconditions,
                                                               updateTransaction.DeletePatterns,
                                                               updateTransaction.InsertData,
                                                               updateTransaction.DefaultGraphUri, label);
                    return GetJobInfo(storeName, jobId.ToString());
                }
                else
                {
                    var jobId = _serverCore.ProcessTransaction(storeName, updateTransaction.ExistencePreconditions,
                                                               updateTransaction.NonexistencePreconditions,
                                                               updateTransaction.DeletePatterns,
                                                               updateTransaction.InsertData,
                                                               updateTransaction.DefaultGraphUri, label);
                    JobExecutionStatus status = _serverCore.GetJobStatus(storeName, jobId.ToString());
                    status.WaitEvent.WaitOne();
                    return new JobInfoObject(status);
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Queing Transaction {0} {1} {2}",
                                 storeName, updateTransaction.DeletePatterns, updateTransaction.InsertData);
                throw new BrightstarClientException(
                    "Error queing transaction in store " + storeName + ". " + ex.Message, ex);
            }
        }
#endif

#if PORTABLE
        public IJobInfo ExecuteUpdate(string storeName, string updateExpression, string label = null)
        {
            try
            {
                var jobId = _serverCore.ExecuteUpdate(storeName, updateExpression, label);
                return GetJobInfo(storeName, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing SPARQL update {0} {1}", storeName, updateExpression);
                throw new BrightstarClientException("Error queing SPARQL update in store " + storeName + ". " + ex.Message, ex);
            }
        }
#else
        /// <summary>
        /// Execute a SPARQL Update expression against a store
        /// </summary>
        /// <param name="storeName">The name of the store to be updated</param>
        /// <param name="updateExpression">The SPARQL Update expression to be applied</param>
        /// <param name="waitForCompletion">If set to true, the method will block until the transaction completes</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>An <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteUpdate(string storeName, string updateExpression, bool waitForCompletion = true, string label = null)
        {
            try
            {
                if (!waitForCompletion)
                {
                    var jobId = _serverCore.ExecuteUpdate(storeName, updateExpression, label);
                    return GetJobInfo(storeName, jobId.ToString());
                } else
                {
                    var jobId = _serverCore.ExecuteUpdate(storeName, updateExpression);
                    var status = _serverCore.GetJobStatus(storeName, jobId.ToString());
                    status.WaitEvent.WaitOne();
                    return new JobInfoObject(status);
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing SPARQL update {0} {1}", storeName, updateExpression);
                throw new BrightstarClientException("Error queing SPARQL update in store " + storeName + ". " + ex.Message, ex);
            }
        }
#endif
        /// <summary>
        /// Gets information about jobs recently executed against a store
        /// </summary>
        /// <param name="storeName">The name of the store to retrieve job information from</param>
        /// <param name="skip">The number of records to skip</param>
        /// <param name="take">The number of records to take</param>
        /// <returns>The subset of job information requested by the skip and take parameters</returns>
        /// <remarks>Job information is returned in reverse order of the order in which they will be / were executed (most recent first).</remarks>
        public IEnumerable<IJobInfo> GetJobInfo(string storeName, int skip, int take)
        {
            if (storeName == null) throw new ArgumentNullException("storeName", Strings.BrightstarServiceClient_StoreNameMustNotBeNull);
            if (String.Empty.Equals(storeName)) throw new ArgumentException(Strings.BrightstarServiceClient_StoreNameMustNotBeEmptyString, "storeName");
            if (skip < 0) throw new ArgumentException(Strings.BrightstarServiceClient_SkipMustNotBeNegative, "skip");
            if (take <= 0) throw new ArgumentException(Strings.BrightstarServiceClient_TakeMustBeGreaterThanZero, "take");
            try
            {
                var jobs = _serverCore.GetJobs(storeName).Skip(skip).Take(take);
                return jobs.Select(jobStatus => new JobInfoObject(jobStatus)).Cast<IJobInfo>();
            }
            catch (NoSuchStoreException)
            {
                throw new BrightstarClientException(String.Format(Strings.BrightstarServiceClient_StoreDoesNotExist, storeName));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting job listing for store '{0}'", storeName);
                throw new BrightstarClientException("Error getting job listing for store " + storeName + ". " + ex.Message, ex);
            }
        }


        /// <summary>
        /// Gets the information about a job. Including status and any messages.
        /// </summary>
        /// <param name="storeName">Name of the store where the job is running</param>
        /// <param name="jobId">The Id of the job</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo GetJobInfo(string storeName, string jobId)
        {
            try
            {
                var jobStatus = _serverCore.GetJobStatus(storeName, jobId);
                return new JobInfoObject(jobStatus);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting data job info {0} {1}", storeName, jobId);
                throw new BrightstarClientException("Error getting job info in store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Starts an import job.
        /// </summary>
        /// <param name="storeName">The store to perform the import to</param>
        /// <param name="fileName">The name of the file in brighhtstar\import folder to import.</param>
        /// <param name="graphUri">The URI of the graph that the data will be imported into.</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <param name="importFormat">The format of the RDF file to be imported.</param>
        /// <returns>An IJobInfo instance</returns>
        public IJobInfo StartImport(string storeName, string fileName, string graphUri = Constants.DefaultGraphUri, string label = null, RdfFormat importFormat = null)
        {
            if (String.IsNullOrEmpty(storeName)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "storeName");
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "fileName");
            if (String.IsNullOrEmpty(graphUri)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "graphUri");
            try
            {
                var jobId = _serverCore.Import(storeName, fileName, graphUri, importFormat, label);
                return GetJobInfo(storeName, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing import job {0} {1}", storeName, fileName);
                throw new BrightstarClientException("Error queing import job in store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Starts an export job
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="fileName">The name of the file in the brightstar\import folder to write to. This file will be overwritten if it already exists.</param>
        /// <param name="graphUri">The URI of the graph to be exported. If NULL, all graphs in the store are exported.</param>
        /// <param name="exportFormat">The <see cref="BrightstarDB.RdfFormat"/> to when serializing the export data. Currently only NQuads and NTriples are supported. Defaults to NQuads</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartExport(string store, string fileName, string graphUri, RdfFormat exportFormat = null, string label = null)
        {
            if (String.IsNullOrEmpty(store)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "store");
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "fileName");
            if (exportFormat == null) exportFormat = RdfFormat.NQuads;
            try
            {
                var jobId = _serverCore.Export(store, fileName, graphUri, exportFormat, label);
                return GetJobInfo(store, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing export job {0} {1}", store,
                                 fileName);
                throw new BrightstarClientException("Error queing export job in store " + store + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Creates a new data file for the specified store that contains only the data required for the current state.
        /// </summary>
        /// <param name="store">Store name</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>An IJobInfo instance</returns>
        public IJobInfo ConsolidateStore(string store, string label = null)
        {
            try
            {
                var jobId = _serverCore.Consolidate(store, label);
                return GetJobInfo(store, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing consolidate job {0}", store);
                throw new BrightstarClientException("Error queing export job in store " + store + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// returns commit points in batches 
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="skip">How many commit points to skip over</param>
        /// <param name="take">Max allowed value is 100</param>
        /// <returns>A enumeration of commit points.</returns>
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, int skip = 0, int take = 100)
        {
            try
            {
                if (take > 100) take = 100;
                var commitPoints = _serverCore.GetCommitPoints(storeName).Skip(skip).Take(take);
// ReSharper disable RedundantEnumerableCastCall
// not redundant for SILVERLIGHT build
                return commitPoints.Select(c => new CommitPointInfoObject {Id = c.LocationOffset, CommitTime = c.CommitTime, JobId = c.JobId, StoreName = storeName}).Cast<ICommitPointInfo>();
// ReSharper restore RedundantEnumerableCastCall
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Get a list of commit points that lie within a specified date/time range
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="latest">The latest commit date of commit points to be retrieved</param>
        /// <param name="earliest">The earliest commit date of the commit points to be retrieved</param>
        /// <param name="skip">The offset into the results list to return from</param>
        /// <param name="take">The number of results to return</param>
        /// <returns></returns>
        /// <remarks>A maximum of 100 commit points are returned in a batch, if <paramref name="take"/> is greater than 100 its value is ignored and only 100 results returned.</remarks>
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, DateTime latest, DateTime earliest, int skip, int take)
        {
            try
            {
                if (take > 100) take = 100;
                DateTime latestUtc = latest.ToUniversalTime();
                DateTime earliestUtc = earliest.ToUniversalTime();
                var commitPoints =
                    _serverCore.GetCommitPoints(storeName).SkipWhile(x => x.CommitTime > latestUtc).TakeWhile(
                        x => x.CommitTime > earliestUtc).Skip(skip).Take(take);
// ReSharper disable RedundantEnumerableCastCall
// not redundant for SILVERLIGHT build
                return commitPoints.Select(c => new CommitPointInfoObject {Id = c.LocationOffset, CommitTime = c.CommitTime, JobId = c.JobId, StoreName = storeName}).Cast<ICommitPointInfo>();
// ReSharper restore RedundantEnumerableCastCall
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves the most recent statistics for the specified store
        /// </summary>
        /// <param name="storeName">The name of the store to retrieve statistics for.</param>
        /// <returns>A <see cref="IStoreStatistics"/> instance containing the most recent statistics for the named store, or NULL if
        /// there are no statistics availabe for the store.</returns>
        public IStoreStatistics GetStatistics(string storeName)
        {
            try
            {
                return _serverCore.GetStatistics(storeName).Select(
                    s =>
                    new StoreStatisticsObject
                        {
                            CommitId = s.CommitNumber,
                            CommitTimestamp = s.CommitTime,
                            TotalTripleCount = s.TripleCount,
                            PredicateTripleCounts = s.PredicateStatistics.ToDictionary(x=>x.Key, x=>x.Value.TripleCount),
                        }
                    ).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting statistics for store {0}", storeName);
                throw new BrightstarClientException("Error getting statistics for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Retrieves a range of statistics records for a store
        /// </summary>
        /// <param name="storeName">The name of the store to retrieve statistics for</param>
        /// <param name="latest">The latest date to retrieve statistics for</param>
        /// <param name="earlierst">The earliest date to retrieve statisitcs for</param>
        /// <param name="skip">The offset into the date-filters list to return from</param>
        /// <param name="take">The number of results to return</param>
        /// <returns>An enumeration over the specified subset of statistics records for the store.</returns>
        /// <exception cref="ArgumentException">Raised if <paramref name="skip"/> is less than 0 or <paramref name="take"/> is greater than 100.</exception>
        public IEnumerable<IStoreStatistics> GetStatistics(string storeName, DateTime latest, DateTime earlierst,
                                                           int skip, int take)
        {
            if (skip <0) throw new ArgumentOutOfRangeException("skip", Strings.BrightstarServiceClient_SkipMustNotBeNegative);
            if (take > 100) throw new ArgumentOutOfRangeException("take", Strings.BrightstarServiceClient_GetStatistics_TakeTooLarge);
            try
            {
                // ReSharper disable RedundantEnumerableCastCall
                // not redundant for SILVERLIGHT build
                return _serverCore.GetStatistics(storeName)
                                  .Where(s => s.CommitTime <= latest && s.CommitTime >= earlierst)
                                  .Skip(skip)
                                  .Take(take)
                                  .Select(s => new StoreStatisticsObject
                                      {
                                          CommitId = s.CommitNumber,
                                          CommitTimestamp = s.CommitTime,
                                          TotalTripleCount = s.TripleCount,
                                          PredicateTripleCounts = s.PredicateStatistics.ToDictionary(x=>x.Key, x=>x.Value.TripleCount)
                                      }).Cast<IStoreStatistics>();
                // ReSharper restore RedundantEnumerableCastCall
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting statistics for store {0}",
                                 storeName);
                throw new BrightstarClientException(
                    "Error getting statistics for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Queues a job to update the statistics for a store
        /// </summary>
        /// <param name="storeName">The name of the store whose statistics are to be updated</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="IJobInfo"/> instance for tracking the current status of the job.</returns>
        public IJobInfo UpdateStatistics(string storeName, string label)
        {
            try
            {
                var jobId = _serverCore.UpdateStatistics(storeName, label);
                return GetJobInfo(storeName, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queuing statistics update job for store {0}", storeName);
                throw new BrightstarClientException(
                    "Error queuing statistics update job for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Queues a job to create a snapshot of a store
        /// </summary>
        /// <param name="storeName">The name of the store to take a snapshot of</param>
        /// <param name="targetStoreName">The name of the store to be created to receive the snapshot</param>
        /// <param name="persistenceType">The type of persistence to use for the target store</param>
        /// <param name="sourceCommitPoint">OPTIONAL: the commit point in the source store to take a snapshot from</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="IJobInfo"/> instance for tracking the current status of the job.</returns>
        public IJobInfo CreateSnapshot(string storeName, string targetStoreName,
                                       PersistenceType persistenceType,
                                       ICommitPointInfo sourceCommitPoint = null,
            string label = null)
        {
            try
            {
                var jobId = _serverCore.CreateSnapshot(storeName, targetStoreName,
                                                       persistenceType,
                                                       sourceCommitPoint == null
                                                           ? StoreConstants.NullUlong
                                                           : sourceCommitPoint.Id,
                                                       label);
                return GetJobInfo(storeName, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queuing snapshot job for store {0}",
                                 storeName);
                throw new BrightstarClientException(
                    "Error queuing snapshot job for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Returns the specified commit point of a BrighstarDB store
        /// </summary>
        /// <param name="storeName">The name of the store to open</param>
        /// <param name="commitId">The identifier of the commit point to be returned</param>
        /// <returns>The specified commit point or NULL if no matching commit point was found</returns>
        public ICommitPointInfo GetCommitPoint(string storeName, ulong commitId)
        {
            try
            {
                var commitPoint = _serverCore.GetCommitPoint(storeName, commitId);
                if (commitPoint == null) return null;
                return new CommitPointInfoObject
                    {
                        StoreName = storeName,
                        Id = commitPoint.LocationOffset,
                        CommitTime = commitPoint.CommitTime,
                        JobId = commitPoint.JobId
                    };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException,
                                 "Error getting commit point {0} for store {1}", commitId, storeName);
                throw new BrightstarClientException(
                    String.Format("Error getting commit point {0} for store {1}. {2}", commitId, storeName,
                                  ex.Message), ex);
            }
        }

        /// <summary>
        /// Returns the commit point that was in effect at a given date/time
        /// </summary>
        /// <param name="storeName">The name of the store to search</param>
        /// <param name="timestamp">The time to search for</param>
        /// <returns>An ICommitPointInfo representing the latest commit point in the store that was committed before the date/time specified by <paramref name="timestamp"/>. If
        /// there is no such commit point (either because the store was created after that date/time or the store has been coalesced and historical data removed),
        /// this method will return null.
        /// </returns>
        public ICommitPointInfo GetCommitPoint(string storeName, DateTime timestamp)
        {
            try
            {
                var commitPoint = _serverCore.GetCommitPoint(storeName, timestamp);
                if (commitPoint == null) return null;
                return new CommitPointInfoObject
                                                      {
                                                          StoreName = storeName,
                                                          Id=commitPoint.LocationOffset,
                                                          CommitTime = commitPoint.CommitTime,
                                                          JobId = commitPoint.JobId
                                                      };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException,
                                 "Error getting commit point at date/time {0} for store {1}", timestamp, storeName);
                throw new BrightstarClientException(
                    String.Format("Error getting commit point at date/time {0} for store {1}. {2}", timestamp, storeName,
                                  ex.Message), ex);
            }
        }

        /// <summary>
        /// This will make the commit point provided the new latest commit point. Blocks until the operation is complete.
        /// </summary>
        /// <param name="storeName">Name of the store</param>
        /// <param name="commitPoint">Commit point to revert to.</param>
        public void RevertToCommitPoint(string storeName, ICommitPointInfo commitPoint)
        {
            try
            {
                _serverCore.RevertToCommitPoint(storeName, commitPoint.Id);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error reverting to commit point {0} for store {1}",commitPoint.Id, storeName);
                throw new BrightstarClientException(
                    String.Format("Error reverting to commit point {0} for store {1}. {2}", commitPoint.Id, storeName,
                                  ex.Message), ex);
            }   
        }

        /// <summary>
        /// Gets a list of transations
        /// </summary>
        /// <param name="storeName">Name of store</param>
        /// <param name="skip">Number of transactions to skip</param>
        /// <param name="take">Number of transaction to return</param>
        /// <returns></returns>
        public IEnumerable<ITransactionInfo> GetTransactions(string storeName, int skip, int take)
        {
            try
            {
                return _serverCore.GetTransactions(storeName).Skip(skip).Take(take)
                                  .Select(t => MakeTransactionInfoWrapper(storeName, t));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting transactions for store {0}", storeName);
                throw new BrightstarClientException("Error getting transactions for store " + storeName + ". " + ex.Message, ex);
            }
        }

        private static Client.ITransactionInfo MakeTransactionInfoWrapper(string storeName, Storage.ITransactionInfo t)
        {
            return new TransactionInfoObject
                    {
                        Id = t.DataStartPosition,
                        JobId = t.JobId,
                        StartTime = t.TransactionStartTime,
                        StoreName = storeName,
#if SILVERLIGHT || PORTABLE
                                             Status = (TransactionStatus)((int)t.TransactionStatus),
                                             TransactionType = (TransactionType)((int)t.TransactionType)
#else
                        Status = (TransactionStatus) ((int) t.TransactionStatus),
                        TransactionType = (TransactionType) ((int) t.TransactionType)
#endif
                    };
        }

        /// <summary>
        /// Returns the transaction record creaed by the execution of a specific job against the store
        /// </summary>
        /// <param name="storeName">The name of the store where the job was executed</param>
        /// <param name="jobId">The ID of the job that was executed</param>
        /// <returns>The transaction information for the execution of the job or NULL if no matching transaction record was found</returns>
        public ITransactionInfo GetTransaction(string storeName, Guid jobId)
        {
            var txnMatch = _serverCore.GetTransactions(storeName).FirstOrDefault(t => t.JobId.Equals(jobId));
            return txnMatch == null ? null : MakeTransactionInfoWrapper(storeName, txnMatch);
        }

        /// <summary>
        /// Executes a previous transaction
        /// </summary>
        /// <param name="storeName">Name of the store</param>
        /// <param name="transactionInfo">Transaction to execute.</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        public IJobInfo ReExecuteTransaction(string storeName, ITransactionInfo transactionInfo, string label = null)
        {
            if (storeName == null) throw new ArgumentNullException("storeName");
            if (transactionInfo == null) throw new ArgumentNullException("transactionInfo");
            try
            {
                var jobId = _serverCore.ReExecuteTransaction(storeName, transactionInfo.Id,
                                                             transactionInfo.TransactionType,
                                                             label);
                return GetJobInfo(storeName, jobId.ToString());
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error rexecuting transaction with JobId {0} for store {1}", transactionInfo.JobId, storeName);
                throw new BrightstarClientException(String.Format("Error rexecuting transaction with JobId {0} for store {1}. {2}", transactionInfo.JobId, storeName, ex.Message), ex);
            }
        }

        #endregion

        /// <summary>
        /// Shutsdown this embedded service only.
        /// </summary>
        /// <param name="completeJobs">If true then the call will block until all jobs queued by the service
        /// have been completed.</param>
        public void Shutdown(bool completeJobs)
        {
            _serverCore.Shutdown(completeJobs);
        }
    }
}
