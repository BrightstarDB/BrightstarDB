using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using BrightstarDB.Server;
using VDS.RDF;
using VDS.RDF.Query;
#if !PORTABLE && !WINDOWS_PHONE && !NETCORE
using System.ServiceModel.Security.Tokens;
using System.Web.Script.Serialization;
#endif
using BrightstarDB.Caching;
using BrightstarDB.Client.RestSecurity;
using BrightstarDB.Dto;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// .NET wrapper for the Brightstar REST API
    /// </summary>
    public class BrightstarRestClient : IBrightstarService
    {
        private const string JsonContentType = "application/json";
        private const string UrlEncodedFormContentType = "application/x-www-form-urlencoded";
        private const int DefaultPollInterval = 500;
        private const int DefaultPollTimeout = 0;

        private readonly Uri _serviceEndpoint;
        private readonly IRequestAuthenticator _requestAuthenticator;
        private int _pollInterval = DefaultPollInterval;
        private int _pollTimeout = DefaultPollTimeout;

        private ICache _clientCache;

        /// <summary>
        /// Get or set the amount of time (in milliseconds)
        /// to wait between poll requests on a job
        /// </summary>
        public int PollInterval
        {
            get { return _pollInterval; }
            set { if (value <= 0) throw new ArgumentException("Poll interval must be greater than 0");
                _pollInterval = value;
            }
        }

        /// <summary>
        /// Get or set the amount of time (in milliseconds)
        /// to wait for the completion of a job.
        /// </summary>
        /// <remarks>A value of 0 indicates that the client
        /// should wait indefinitely for completion.</remarks>
        public int PollTimeout
        {
            get { return _pollTimeout; }
            set
            {
                if (value < 0) throw new ArgumentException("Poll timeout must be greater than or equal to 0");
                _pollTimeout = value;
            }
        }

        internal BrightstarRestClient(string serviceEndpoint, IRequestAuthenticator requestAuthenticator, ICache clientCache)
        {
            _serviceEndpoint = serviceEndpoint.EndsWith("/") ? new Uri(serviceEndpoint) : new Uri(serviceEndpoint + "/");
            _requestAuthenticator = requestAuthenticator;
            _clientCache = clientCache;
        }

        internal BrightstarRestClient(string serviceEndpoint, string accountId, string authenticationKey)
        {
            _serviceEndpoint = serviceEndpoint.EndsWith("/") ? new Uri(serviceEndpoint) : new Uri(serviceEndpoint + "/");
            _requestAuthenticator = new SharedSecretAuthenticator(accountId, authenticationKey);
            _clientCache = new NullCache();
        }

        #region Implementation of IBrightstarService

        /// <summary>
        /// Returns the timestamp provided by the server on its last response.
        /// </summary>
        /// <remarks>This property will be null if no operation has been invoked, or 
        /// if the client is an embedded client.</remarks>
        public DateTime? LastResponseTimestamp { get; private set; }

        /// <summary>
        /// List the names of the stores managed by this Brightstar server
        /// </summary>
        /// <returns>An enumeration over the names of the stores managed by the Brightstar server</returns>
        public IEnumerable<string> ListStores()
        {
            var response = AuthenticatedGet("");
            var storesResponse = Deserialize<StoresResponseModel>(response);
            return storesResponse.Stores;
        }

        
        /// <summary>
        /// Create a new store
        /// </summary>
        /// <param name="storeName">The name of the store to be created</param>
        public void CreateStore(string storeName)
        {
            CreateStore(new CreateStoreRequestObject(storeName));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="persistenceType"></param>
        public void CreateStore(string storeName, PersistenceType persistenceType)
        {
            CreateStore(new CreateStoreRequestObject(storeName, persistenceType));
        }

        private void CreateStore(CreateStoreRequestObject storeRequestObject)
        {
            try
            {
                ValidateStoreName(storeRequestObject.StoreName);
                var response = AuthenticatedPost("", storeRequestObject);
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new BrightstarClientException(
                        String.Format("Store not created. Server response was: {0} - {1}", response.StatusCode,
                                      response.StatusDescription));
                }
            }
            catch (BrightstarClientException ex)
            {
                if (InnerExceptionHasStatusCode(ex, HttpStatusCode.Conflict))
                {
                    throw new BrightstarClientException(Strings.BrightstarServiceClient_StoreNameConflict);
                }
                throw;
            }
            catch (WebException wex)
            {
                if (wex.Response is HttpWebResponse)
                {
                    var httpWebResponse = wex.Response as HttpWebResponse;
                    if (httpWebResponse.StatusCode == HttpStatusCode.Conflict)
                    {
                        throw new BrightstarClientException(Strings.BrightstarServiceClient_StoreNameConflict);
                    }
                }
            }
        }

        private static void ValidateStoreName(string storeName, string argName = "storeName")
        {
            if (storeName == null) throw new ArgumentNullException(argName, Strings.BrightstarServiceClient_StoreNameMustNotBeNull);
            if (String.IsNullOrEmpty(storeName)) throw new ArgumentException(Strings.BrightstarServiceClient_StoreNameMustNotBeEmptyString, argName);
            if (!System.Text.RegularExpressions.Regex.IsMatch(storeName, Constants.StoreNameRegex))
            {
                throw new ArgumentException(Strings.BrightstarServiceClient_InvalidStoreName, argName);
            }
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public void DeleteStore(string storeName)
        {
            ValidateStoreName(storeName);
            var response = AuthenticatedDelete(storeName);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new BrightstarClientException(
                    String.Format("Store not deleted. Server response was: {0} - {1}", response.StatusCode,
                                  response.StatusDescription));
            }
        }

        /// <summary>
        /// Checks to see if the named store already exists
        /// </summary>
        /// <param name="storeName">The name of the store to test for</param>
        /// <returns>True if store exists, false otherwise</returns>
        public bool DoesStoreExist(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                AuthenticatedHead(storeName);
                return true;
            }
            catch (BrightstarClientException ex)
            {
                if (InnerExceptionHasStatusCode(ex, HttpStatusCode.NotFound))
                {
                    return false;
                }
                throw;
            }
            catch (WebException wex)
            {
                var response = wex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
                var webExceptionDetail = GetAndLogWebExceptionDetail("HEAD", storeName, wex);
                throw new BrightstarClientException(String.Format("Could not verify existence of store - '{0}'",webExceptionDetail), wex);                
            }
        }

        /// <summary>
        /// List the URIs of the named graphs contained in the specified store
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <returns>An enumeration of the URI identifiers of the named graphs in the store.</returns>
        public IEnumerable<string> ListNamedGraphs(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                var response = AuthenticatedGet(storeName + "/graphs");
                return Deserialize <List<string>>(response);
            }
            catch (BrightstarClientException ex)
            {
                if (InnerExceptionHasStatusCode(ex, HttpStatusCode.NotFound))
                {
                    throw new NoSuchStoreException(storeName);
                }
                throw;
            }
            catch (WebException wex)
            {
                var response = wex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new NoSuchStoreException(storeName);
                }
                var webExceptionDetail = GetAndLogWebExceptionDetail("GET", storeName + "/graphs", wex);
                throw new BrightstarClientException(
                    String.Format("Could not retrieve named graphs for store '{0}' - '{1}'.",storeName,webExceptionDetail), wex);
            }
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a <see cref="BrightstarClientException"/> will be raised with the message "Store not modified".</param>
        /// <param name="defaultGraphUris">An enumeration over the URIs of the graphs that will be taken together as the default graph for the query. May be NULL to use the built-in default graph</param>
        /// <param name="resultsFormat">Specifies the serialization format for the SPARQL result set returned by the query. May be NULL to indicate that an RDF graph is the expected result.</param>
        /// <param name="graphFormat">Specifies the serialization format for the RDF graph returned by the query. May be NULL to indicate that a SPARQL results set is the expected result.</param>
        /// <param name="streamFormat">Specifies the serialization format used in the returned <see cref="Stream"/>.</param>
        /// <returns>A stream containing the results of executing the query</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression, IEnumerable<string> defaultGraphUris, DateTime? ifNotModifiedSince,
                                   SparqlResultsFormat resultsFormat, RdfFormat graphFormat, out ISerializationFormat streamFormat)
        {
            // Parameter validation
            ValidateStoreName(storeName);
            if (queryExpression == null) throw new ArgumentNullException("queryExpression", Strings.StringParameterMustBeNonEmpty);
            if (String.IsNullOrEmpty(queryExpression)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "queryExpression");
            if (resultsFormat == null && graphFormat == null) throw new ArgumentException("Either a SparqlResultsFormat or an RdfFormat (or both) must be specified");

            string cacheKey = null;
            var graphs = defaultGraphUris == null ? null : defaultGraphUris.ToArray();
            CachedQueryResult cachedResult = null;
            ISerializationFormat cachedResultFormat = null;

            if (ifNotModifiedSince == null && _clientCache != null)
            {
                // See if we have a cached result
                cacheKey = storeName + "_" + queryExpression.GetHashCode();
                if (graphs != null)
                {
                    cacheKey = cacheKey + "_" + String.Join(",", graphs).GetHashCode();
                }
                if (resultsFormat != null)
                {
                    var cachedResultBytes = _clientCache.Lookup(cacheKey + "_" + resultsFormat);
                    cachedResult = cachedResultBytes == null ? null : CachedQueryResult.FromBinary(cachedResultBytes);
                    if (cachedResult != null) cachedResultFormat = resultsFormat;
                }
                if (cachedResult == null && graphFormat != null)
                {
                    var cachedResultBytes = _clientCache.Lookup(cacheKey + "_" + graphFormat);
                    cachedResult = cachedResultBytes == null ? null : CachedQueryResult.FromBinary(cachedResultBytes);
                    if (cachedResult != null) cachedResultFormat = graphFormat;
                }
                if (cachedResult != null)
                {
                    ifNotModifiedSince = cachedResult.Timestamp;
                }
            }

            if (ifNotModifiedSince.HasValue)
            {
                // Check if store has been modified
                var headResponse = AuthenticatedHead(storeName);
                var lastModified = GetLastModified(headResponse);
                if (lastModified.HasValue && lastModified <= ifNotModifiedSince.Value)
                {
                    if (cachedResult != null)
                    {
                        // Cached result is still valid
                        streamFormat = cachedResultFormat;
                        return new MemoryStream(
                            resultsFormat == null
                            ? Encoding.UTF8.GetBytes(cachedResult.Result)
                            : resultsFormat.Encoding.GetBytes(cachedResult.Result));
                    }
                    throw new BrightstarStoreNotModifiedException();
                }
            }

            // Construct query request as a form POST
            var parameters = new List<Tuple<string, string>> { new Tuple<string, string>("query", queryExpression) };
            if (defaultGraphUris != null)
            {
                parameters.AddRange(
                    graphs.Where(x => !string.IsNullOrEmpty(x))
                        .Select(g => new Tuple<string, string>("default-graph-uri", g)));
            }

            // Execute
            var accept = MakeAcceptHeader(resultsFormat, graphFormat);

            var queryResponse = AuthenticatedFormPost(storeName + "/sparql", parameters, accept);
            var responseStream = queryResponse.GetResponseStream();
            streamFormat = (ISerializationFormat) SparqlResultsFormat.GetResultsFormat(queryResponse.ContentType) ??
                           RdfFormat.GetResultsFormat(queryResponse.ContentType);

            // Cache result and return
            if (_clientCache != null && cacheKey != null && LastResponseTimestamp.HasValue && responseStream != null)
            {
                using (var streamReader = new StreamReader(responseStream))
                {
                    var resultString = streamReader.ReadToEnd();
                    cachedResult = new CachedQueryResult(LastResponseTimestamp.Value, resultString);

                    _clientCache.Insert(cacheKey + "_" + streamFormat, cachedResult.ToBinary(), CachePriority.Normal);
                    return new MemoryStream(streamReader.CurrentEncoding.GetBytes(cachedResult.Result));
                }
            }
            else
            {
                return responseStream;
            }

        }

        private static string MakeAcceptHeader(SparqlResultsFormat resultsFormat, RdfFormat graphFormat)
        {
            string accept = String.Empty;
            if (resultsFormat != null)
            {
                accept = resultsFormat.ToString();
                if (graphFormat != null)
                {
                    accept += ", " + graphFormat.ToString();
                }
            }
            else if (graphFormat != null)
            {
                accept = graphFormat.ToString();
            }
            return accept;
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
            return ExecuteQuery(storeName, queryExpression, (string[]) null, ifNotModifiedSince, resultsFormat, graphFormat);
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
            return ExecuteQuery(storeName, queryExpression, defaultGraphUri == null ? null : new[] {defaultGraphUri},
                ifNotModifiedSince, resultsFormat, graphFormat);
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="defaultGraphUris">An enumeration over the URIs of the graphs that will be taken together as the default graph for the query</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a <see cref="BrightstarStoreNotModifiedException"/> will be raised.</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <param name="graphFormat">OPTIONAL: Specifies the serialization format for RDF graph results. Defaults to <see cref="RdfFormat.RdfXml"/></param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="storeName"/> or <paramref name="queryExpression"/> is NULL</exception>
        /// <exception cref="ArgumentException">If <paramref name="storeName"/> or <paramref name="queryExpression"/> is an empty string</exception>
        /// <exception cref="BrightstarStoreNotModifiedException">Raised if the <paramref name="ifNotModifiedSince"/> parameter has 
        /// a value and the store has not been modified since the specified time.</exception>
        public Stream ExecuteQuery(string storeName, string queryExpression,
                                   IEnumerable<string> defaultGraphUris,
                                   DateTime? ifNotModifiedSince = new DateTime?(),
                                   SparqlResultsFormat resultsFormat = null,
                                   RdfFormat graphFormat = null)
        {
            ISerializationFormat streamFormat;
            return ExecuteQuery(storeName, queryExpression, defaultGraphUris, ifNotModifiedSince,
                                resultsFormat ?? SparqlResultsFormat.Xml, graphFormat ?? RdfFormat.RdfXml,
                                out streamFormat);
        }


        private static DateTime? GetLastModified(HttpWebResponse r)
        {
#if PORTABLE || WINDOWS_PHONE || NETCORE
            var headerVal = r.Headers["Last-Modified"];
            DateTime lastModified;
            if (!String.IsNullOrEmpty(headerVal) && DateTime.TryParse(headerVal, out lastModified))
            {
                return lastModified;
            }
            return null;
#else
            return r.LastModified;
#endif
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
            return ExecuteQuery(commitPoint, queryExpression, (IEnumerable<string>) null, resultsFormat, graphFormat);
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
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression, string defaultGraphUri,
                                   SparqlResultsFormat resultsFormat = null, RdfFormat graphFormat = null)
        {
            return ExecuteQuery(commitPoint, queryExpression, new string[] {defaultGraphUri}, resultsFormat, graphFormat);
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
            if (commitPoint == null) throw new ArgumentNullException("commitPoint");
            ValidateStoreName(commitPoint.StoreName, "commitPoint.StoreName");
            if (queryExpression == null) throw new ArgumentNullException("queryExpression");
            if (resultsFormat == null && graphFormat == null) throw new ArgumentException("Either a SparqlResultsFormat or an RdfFormat (or both) must be specified");

            if (String.IsNullOrWhiteSpace(queryExpression))
                throw new ArgumentException(Strings.BrightstarServiceClient_QueryMustNotBeEmptyString, "queryExpression");
            var queryUri = commitPoint.StoreName + "/commits/" + commitPoint.Id + "/sparql";
            var postParameters = new List<Tuple<string, string>> { new Tuple<string, string>("query", queryExpression) };
            if (defaultGraphUris != null)
            {
                postParameters.AddRange(
                    defaultGraphUris.Where(x => !string.IsNullOrEmpty(x))
                        .Select(graphUri => new Tuple<string, string>("default-graph-uri", graphUri)));
            }
            var queryResponse = AuthenticatedFormPost(queryUri, postParameters, MakeAcceptHeader(resultsFormat, graphFormat));
            streamFormat = SparqlResultsFormat.GetResultsFormat(queryResponse.ContentType) ??
                           (ISerializationFormat) RdfFormat.GetResultsFormat(queryResponse.ContentType);
            return queryResponse.GetResponseStream();
        }

#if PORTABLE
        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions">NTriples that must be in the store in order for the transaction to execute</param>
        /// <param name="deletePatterns">The delete patterns that will be removed from the store</param>
        /// <param name="insertData">The NTriples data that will be inserted into the store.</param>
        /// <param name="defaultGraphUri">The URI of the default graph to be updated by the transaction.</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>Job Info</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="storeName"/> is NULL</exception>
        /// <exception cref="ArgumentException">If <paramref name="storeName"/> is an empty string or not a valid store name</exception>
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns,
                                           string insertData, string defaultGraphUri, string label = null)
        {
            return ExecuteTransaction(storeName,
                                      new UpdateTransactionData
                                          {
                                              ExistencePreconditions = preconditions,
                                              DeletePatterns = deletePatterns,
                                              InsertData = insertData,
                                              DefaultGraphUri = defaultGraphUri
                                          }, label);
        }

        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="updateTransaction">The update transaction data</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="JobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteTransaction(string storeName, UpdateTransactionData updateTransaction, string label)
        {
            ValidateStoreName(storeName);
            var transactionJob = JobRequestObject.CreateTransactionJob(updateTransaction, label);
            var jobUri = CreateJob(storeName, transactionJob);
            var jobInfoResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobInfoResponse);
        }
#else
        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions">NTriples that must be in the store in order for the transaction to execute</param>
        /// <param name="deletePatterns">The delete patterns that will be removed from the store</param>
        /// <param name="insertData">The NTriples data that will be inserted into the store.</param>
        /// <param name="defaultGraphUri">The URI of the default graph to be updated by the transaction.</param>
        /// <param name="waitForCompletion">If set to true the method will block until the transaction completes</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>Job Info</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="storeName"/> is NULL</exception>
        /// <exception cref="ArgumentException">If <paramref name="storeName"/> is an empty string or not a valid store name</exception>
        [Obsolete("This method has been superceeded by " +
                  "ExecuteTransaction(storeName, existencePreconditions, nonexistencePreconditions, " +
                  "deletePatterns, insertData, defaultGraphUri, waitForCompletion, label)")]
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns,
                                           string insertData, string defaultGraphUri, bool waitForCompletion = true,
            string label = null)
        {
            return ExecuteTransaction(storeName,
                                      new UpdateTransactionData
                                          {
                                              ExistencePreconditions = preconditions,
                                              NonexistencePreconditions = String.Empty,
                                              DeletePatterns = deletePatterns,
                                              InsertData = insertData,
                                              DefaultGraphUri = defaultGraphUri
                                          }, waitForCompletion, label);
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
            ValidateStoreName(storeName);
            var transactionJob = JobRequestObject.CreateTransactionJob(updateTransaction, label);
            var jobUri = CreateJob(storeName, transactionJob);
            if (waitForCompletion)
            {
                return PollJob(jobUri);
            }
            var jobInfoResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobInfoResponse);
        }
#endif

#if PORTABLE
        /// <summary>
        /// Execute a SPARQL Update expression against a store
        /// </summary>
        /// <param name="storeName">The name of the store to be updated</param>
        /// <param name="updateExpression">The SPARQL Update expression to be applied</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteUpdate(string storeName, string updateExpression, string label = null)
        {
            ValidateStoreName(storeName);
            if (String.IsNullOrWhiteSpace(updateExpression))
                throw new ArgumentException(Strings.BrightstarServiceClient_UpdateExpressionMustNotBeEmptyString,
                                            "updateExpression");
            var job = JobRequestObject.CreateSparqlUpdateJob(updateExpression, label);
            var jobUri = CreateJob(storeName, job);
            var jobResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobResponse);        
        }
#else
        /// <summary>
        /// Execute a SPARQL Update expression against a store
        /// </summary>
        /// <param name="storeName">The name of the store to be updated</param>
        /// <param name="updateExpression">The SPARQL Update expression to be applied</param>
        /// <param name="waitForCompletion">If set to true, the method will block until the transaction completes</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteUpdate(string storeName, string updateExpression, bool waitForCompletion = true, string label = null)
        {
            ValidateStoreName(storeName);
            if (String.IsNullOrWhiteSpace(updateExpression))
                throw new ArgumentException(Strings.BrightstarServiceClient_UpdateExpressionMustNotBeEmptyString,
                                            "updateExpression");
            var job = JobRequestObject.CreateSparqlUpdateJob(updateExpression, label);
            var jobUri = CreateJob(storeName, job);
            if (waitForCompletion)
            {
                return PollJob(jobUri);
            }
            var jobResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobResponse);
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
            ValidateStoreName(storeName);
            if (skip < 0) throw new ArgumentException(Strings.BrightstarServiceClient_SkipMustNotBeNegative, "skip");
            if (take <= 0) throw new ArgumentException(Strings.BrightstarServiceClient_TakeMustBeGreaterThanZero, "take");
            if (take > 100) throw new ArgumentException(Strings.BrightstarServiceClient_GetJobInfo_TakeToLarge, "take");

            var queryUri = String.Format("{0}/jobs?skip={1}&take={2}", storeName, skip, take);
            var response = AuthenticatedGet(queryUri);
#if WINDOWS_PHONE || SILVERLIGHT4
            return Deserialize<List<JobResponseModel>>(response).Cast<IJobInfo>();
#else
            return Deserialize<List<JobResponseModel>>(response);
#endif
        }

        /// <summary>
        /// Gets the information about a job. Including status and any messages.
        /// </summary>
        /// <param name="storeName">Name of the store where the job is running</param>
        /// <param name="jobId">The Id of the job</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo GetJobInfo(string storeName, string jobId)
        {
            ValidateStoreName(storeName);
            var jobInfoResponse = AuthenticatedGet(storeName + "/jobs/" + jobId);
            return Deserialize<JobResponseModel>(jobInfoResponse);
        }

        /// <summary>
        /// Starts an import job
        /// </summary>
        /// <param name="store">The store to import into</param>
        /// <param name="fileName">The URI of the data to import</param>
        /// <param name="graphUri">The URI identifier of the graph that the data is to be imported into. If NULL, import is into the default graph</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <param name="importFormat">The format of the data to import</param>
        /// <returns>A <see cref="IJobInfo"/> instance to use for monitoring the progress of the job</returns>
        public IJobInfo StartImport(string store, string fileName, string graphUri, string label = null, RdfFormat importFormat = null)
        {
            ValidateStoreName(store);
            var job = JobRequestObject.CreateImportJob(fileName, graphUri, label, importFormat);
            var jobUri = CreateJob(store, job);
            var jobInfoResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobInfoResponse);
        }

        /// <summary>
        /// Starts an export job
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="fileName">The name of the file in the brightstar\import folder to write to. This file will be overwritten if it already exists.</param>
        /// <param name="graphUri">The identifier of the store graph to be exported. If NULL, all graphs in the store will be exported.</param>
        /// <param name="exportFormat">The serialization format to use for the exported data. If unspecified or null, export will default to using NQuads format. </param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartExport(string store, string fileName, string graphUri, RdfFormat exportFormat = null, string label = null)
        {
            ValidateStoreName(store);
            var job = JobRequestObject.CreateExportJob(fileName, graphUri, exportFormat, label);
            var jobUri = CreateJob(store, job);
            var jobInfoResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobInfoResponse);
        }

        /// <summary>
        /// Creates a new store file containing only the data required for the current state
        /// </summary>
        /// <param name="store">The store to consolidate</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo ConsolidateStore(string store, string label = null)
        {
            ValidateStoreName(store);
            var job = JobRequestObject.CreateConsolidateJob(label);
            var jobUri = CreateJob(store, job);
            var jobInfoResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobInfoResponse);
        }

        /// <summary>
        /// Returns the commit points of a Brightstar store
        /// </summary>
        /// <param name="storeName">The name of the store to examine</param>
        /// <param name="skip">How many commit points to skip over</param>
        /// <param name="take">How many commit points to return. Max allowed value is 100</param>
        /// <returns>An enumeration over the store commit points</returns>
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, int skip, int take)
        {
            ValidateStoreName(storeName);
            if (take > 100) throw new ArgumentException(Strings.BrightstarServiceClient_GetCommitPoints_TakeToLarge, "take");
            if (take < 1) throw new ArgumentException(Strings.BrightstarServiceClient_TakeMustBeGreaterThanZero, "take");
            if (skip < 0) throw new ArgumentException(Strings.BrightstarServiceClient_SkipMustNotBeNegative, "skip");
            var commitPointsResponse =
                AuthenticatedGet(storeName + String.Format("/commits?skip={0}&take={1}", skip, take));
            var results = Deserialize<List<CommitPointResponseModel>>(commitPointsResponse);
            if (results != null) return results.Cast<ICommitPointInfo>();
            throw new BrightstarClientException(Strings.BrightstarServiceClient_UnexpectedResponseContent);
        }

        /// <summary>
        /// Returns the specified commit point of a BrighstarDB store
        /// </summary>
        /// <param name="storeName">The name of the store to open</param>
        /// <param name="commitId">The identifier of the commit point to be returned</param>
        /// <returns>The specified commit point or NULL if no matching commit point was found</returns>
        public ICommitPointInfo GetCommitPoint(string storeName, ulong commitId)
        {
            ValidateStoreName(storeName);
            try
            {
                var commitPointResponse = AuthenticatedGet(storeName + "/commits/" + commitId);
                var result = Deserialize<CommitPointResponseModel>(commitPointResponse);
                return result;
            }
            catch (BrightstarClientException clientException)
            {
                if (InnerExceptionHasStatusCode(clientException, HttpStatusCode.NotFound))
                {
                    return null;
                }
                throw;
            }
        }

        private static bool InnerExceptionHasStatusCode(BrightstarClientException clientException, HttpStatusCode expectedCode)
        {
            var webException = clientException.InnerException as WebException;
            if (webException != null)
            {
                var httpResponse = webException.Response as HttpWebResponse;
                if (httpResponse != null && httpResponse.StatusCode == expectedCode)
                {
                    return true;
                }
            }
            return false;
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
            ValidateStoreName(storeName);
            var requestUri = storeName + "/commits?timestamp=" + timestamp.ToString("u");
            try
            {
                var response = AuthenticatedGet(requestUri);
                return Deserialize<CommitPointInfoObject>(response);
            }
            catch (BrightstarClientException ex)
            {
                if (InnerExceptionHasStatusCode(ex, HttpStatusCode.NotFound))
                {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// This will make the commit point provided the new latest commit point. Blocks until the operation is complete.
        /// </summary>
        /// <param name="storeName">The name of the store to be reverted</param>
        /// <param name="commitPoint">The commit point to revert to</param>
        public void RevertToCommitPoint(string storeName, ICommitPointInfo commitPoint)
        {
            ValidateStoreName(storeName);
            if (commitPoint == null) throw new ArgumentNullException("commitPoint");
            if (commitPoint.StoreName != storeName)
            {
                throw new ArgumentException(Strings.BrightstarServiceClient_InvalidCommitPointInfoObject, "commitPoint");
            }

            var postUri = storeName + "/commits";
            var postCommit = new CommitPointInfoObject
                {
                    Id = commitPoint.Id,
                    StoreName = commitPoint.StoreName,
                    CommitTime = commitPoint.CommitTime,
                    JobId = commitPoint.JobId
                };
            AuthenticatedPost(postUri, postCommit);
        }

        /// <summary>
        /// Gets a list of transactions executed against this store
        /// </summary>
        /// <param name="storeName">Name of store for </param>
        /// <param name="skip">How many transactions to skip over</param>
        /// <param name="take">How many transactions to return.</param>
        /// <returns>An enumeration over the transactions executed against the store</returns>
        public IEnumerable<ITransactionInfo> GetTransactions(string storeName, int skip, int take)
        {
            ValidateStoreName(storeName);
            if (skip < 0) throw new ArgumentException(Strings.BrightstarServiceClient_SkipMustNotBeNegative, "skip");
            if (take <= 0) throw new ArgumentException(Strings.BrightstarServiceClient_TakeMustBeGreaterThanZero, "take");
            if (take > 100) throw new ArgumentException(Strings.BrightstarServiceClient_GetTransactions_TakeTooLarge, "take");

            var queryUri = String.Format("{0}/transactions?skip={1}&take={2}", storeName, skip, take);
            var response = AuthenticatedGet(queryUri);
#if WINDOWS_PHONE || SILVERLIGHT4
            return Deserialize<List<TransactionInfoObject>>(response).Cast<ITransactionInfo>();
#else
            return Deserialize<List<TransactionInfoObject>>(response);
#endif
        }

        /// <summary>
        /// Returns the transaction record creaed by the execution of a specific job against the store
        /// </summary>
        /// <param name="storeName">The name of the store where the job was executed</param>
        /// <param name="jobId">The ID of the job that was executed</param>
        /// <returns>The transaction information for the execution of the job or NULL if no matching transaction record was found</returns>
        public ITransactionInfo GetTransaction(string storeName, Guid jobId)
        {
            ValidateStoreName(storeName);

            var queryUri = String.Format("{0}/transactions/byjob/{1}", storeName, jobId);
            try
            {
                var response = AuthenticatedGet(queryUri);
                return Deserialize<TransactionInfoObject>(response);
            }
            catch (BrightstarClientException ex)
            {
                if (InnerExceptionHasStatusCode(ex, HttpStatusCode.NotFound))
                {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// Executes a previous transaction
        /// </summary>
        /// <param name="storeName">The name of the store to re-apply the transaction to</param>
        /// <param name="transactionInfo">The transaction to be applied</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        public IJobInfo ReExecuteTransaction(string storeName, ITransactionInfo transactionInfo, string label)
        {
            ValidateStoreName(storeName);
            if (transactionInfo == null) throw new ArgumentNullException("transactionInfo");
            if (transactionInfo.StoreName != storeName) throw new ArgumentException(Strings.BrightstarServiceClient_InvalidTransactionInfoObject, "transactionInfo");

            var job = JobRequestObject.CreateRepeatTransactionJob(transactionInfo.JobId, label);
            var jobUri = CreateJob(storeName, job);
            var jobResponse = AuthenticatedGet(jobUri);
            return Deserialize<JobResponseModel>(jobResponse);
        }

        /// <summary>
        /// Get a list of commit points that lie within a specified date/time range
        /// </summary>
        /// <param name="storeName">The name of the store to examine</param>
        /// <param name="latest">The latest commit date of commit points to be retrieved</param>
        /// <param name="earliest">The earliest commit date of the commit points to be retrieved</param>
        /// <param name="skip">The offset into the results list to return from</param>
        /// <param name="take">The number of results to return</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Raised if <paramref name="skip"/> is less than 0 or <paramref name="take"/> is greater than 100.</exception>
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, DateTime latest, DateTime earliest,
                                                             int skip, int take)
        {
            ValidateStoreName(storeName);
            if (take > 100)
                throw new ArgumentException(Strings.BrightstarServiceClient_GetCommitPoints_TakeToLarge, "take");
            if (take < 1)
                throw new ArgumentException(Strings.BrightstarServiceClient_TakeMustBeGreaterThanZero, "take");
            if (skip < 0) throw new ArgumentException(Strings.BrightstarServiceClient_SkipMustNotBeNegative, "skip");
            if (latest < earliest)
            {
                throw new ArgumentException(Strings.BrightstarServiceClient_InvalidDateRange, "latest");
            }
            var queryUri = String.Format(CultureInfo.InvariantCulture,
                                         "{0}/commits?latest={1}&earliest={2}&skip={3}&take={4}",
                                         storeName, latest.ToString("u"), earliest.ToString("u"),
                                         skip, take);
            var response = AuthenticatedGet(queryUri);
#if WINDOWS_PHONE || SILVERLIGHT4
            return Deserialize<List<CommitPointResponseModel>>(response).Cast<ICommitPointInfo>();
#else
            return Deserialize<List<CommitPointResponseModel>>(response);
#endif
        }

        /// <summary>
        /// Retrieves the most recent statistics for the specified store
        /// </summary>
        /// <param name="storeName">The name of the store to retrieve statistics for.</param>
        /// <returns>A <see cref="IStoreStatistics"/> instance containing the most recent statistics for the named store, or NULL if
        /// there are no statistics availabe for the store.</returns>
        public IStoreStatistics GetStatistics(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                var response = AuthenticatedGet(storeName + "/statistics/latest");
#if WINDOWS_PHONE || SILVERLIGHT4
                return Deserialize<StoreStatisticsObject>(response) as IStoreStatistics;
#else
                return Deserialize<StoreStatisticsObject>(response);
#endif
            }
            catch (BrightstarClientException ex)
            {
                if (InnerExceptionHasStatusCode(ex, HttpStatusCode.NotFound))
                {
                    // Store has no statistics yet
                    return null;
                }
                throw;
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
        public IEnumerable<IStoreStatistics> GetStatistics(string storeName, DateTime latest, DateTime earlierst, int skip, int take)
        {
            ValidateStoreName(storeName);
            if (skip < 0) throw new ArgumentException(Strings.BrightstarServiceClient_SkipMustNotBeNegative);
            if (take > 100) throw new ArgumentException(Strings.BrightstarServiceClient_GetStatistics_TakeTooLarge);
            var uri = String.Format("{0}/statistics?latest={1}&earlies={2}&skip={3}&take={4}",
                                    storeName, latest.ToString("u"), earlierst.ToString("u"), skip, take);
            var response = AuthenticatedGet(uri);
#if WINDOWS_PHONE || SILVERLIGHT4
            return Deserialize<List<StoreStatisticsObject>>(response).Cast<IStoreStatistics>();
#else
            return Deserialize<List<StoreStatisticsObject>>(response);
#endif
        }

        /// <summary>
        /// Queues a job to update the statistics for a store
        /// </summary>
        /// <param name="storeName">The name of the store whose statistics are to be updated</param>
        /// <param name="label">Optional user-friendly label for the job.</param>
        /// <returns>A <see cref="IJobInfo"/> instance for tracking the current status of the job.</returns>
        public IJobInfo UpdateStatistics(string storeName, string label)
        {
            ValidateStoreName(storeName);
            var job = JobRequestObject.CreateUpdateStatsJob(label);
            var jobUri = CreateJob(storeName, job);
            var response = AuthenticatedGet(jobUri);
            var jobInfo = Deserialize<JobResponseModel>(response);
            return jobInfo;
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
        public IJobInfo CreateSnapshot(string storeName, string targetStoreName, PersistenceType persistenceType, ICommitPointInfo sourceCommitPoint = null, string label = null)
        {
            ValidateStoreName(storeName);
            ValidateStoreName(targetStoreName, "targetStoreName");
            var job = sourceCommitPoint == null
                          ? JobRequestObject.CreateSnapshotJob(targetStoreName, persistenceType, label)
                          : JobRequestObject.CreateSnapshotJob(targetStoreName, persistenceType,
                                                               sourceCommitPoint.Id, label);
            var jobUri = CreateJob(storeName, job);
            var jobResponse = AuthenticatedGet(jobUri);
            var jobStatus = Deserialize<JobResponseModel>(jobResponse);
            return jobStatus;
        }

        #endregion

        private HttpWebResponse AuthenticatedGet(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var getRequest = WebRequest.Create(uri.ToString()) as HttpWebRequest;
            if (getRequest == null) throw new ArgumentException(Strings.NotAnHttpRequest);
            getRequest.Accept = JsonContentType;
            SetDateHeader(getRequest);
            _requestAuthenticator.Authenticate(getRequest);

            try
            {
                var ret = getRequest.GetResponse() as HttpWebResponse;
                if (ret != null)
                {
                    LastResponseTimestamp = DateTime.Now;
                }
                return ret;
            }
            catch (WebException wex)
            {
                var webExceptionDetail = GetAndLogWebExceptionDetail("GET", relativePath, wex);
                throw new BrightstarClientException(webExceptionDetail, wex);
            }
        }

        private void SetDateHeader(HttpWebRequest httpWebRequest)
        {
            // NOTE: Currently there doesn't seem to be anyway of setting this
            // property in PCL - trying to set the httpWebRequest.Headers
            // directly results in a runtime error
#if !PORTABLE
#if NETCORE
            httpWebRequest.Headers[HttpRequestHeader.Date] = DateTime.UtcNow.ToString("R");
#else
            httpWebRequest.Date = DateTime.UtcNow;
#endif
#endif
        }

        private HttpWebResponse AuthenticatedHead(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var headRequest = WebRequest.Create(uri.ToString()) as HttpWebRequest;
            if (headRequest == null) throw new ArgumentException(Strings.NotAnHttpRequest);
            headRequest.Method = "HEAD";
            headRequest.Accept = JsonContentType;
            SetDateHeader(headRequest);
            _requestAuthenticator.Authenticate(headRequest);
            try
            {
                return headRequest.GetResponse() as HttpWebResponse;
            }
            catch (WebException wex)
            {
                var webExceptionDetail = GetAndLogWebExceptionDetail("HEAD", relativePath, wex);
                throw new BrightstarClientException(webExceptionDetail, wex);
            }
        }

        private HttpWebResponse AuthenticatedPost<T>(string relativePath, T postDto)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var postRequest = WebRequest.Create(uri) as HttpWebRequest;
            if (postRequest == null) throw new ArgumentException(Strings.NotAnHttpRequest);
            postRequest.Method = "POST";
            SetDateHeader(postRequest);
            postRequest.ContentType = JsonContentType;
            postRequest.Accept = JsonContentType;
            var serializer = new Newtonsoft.Json.JsonSerializer();
            var contentBuilder = new StringBuilder();
            using (var contentWriter = new StringWriter(contentBuilder))
            {
                serializer.Serialize(contentWriter, postDto);
            }
            var content = contentBuilder.ToString().TrimEnd('&');
            using (var writer = new StreamWriter(postRequest.GetRequestStream()))
            {
                writer.Write(content);
            }
#if PORTABLE
            var md5 = MD5.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashCode = Convert.ToBase64String(md5.ComputeHash(contentBytes));
#else
            var md5 = System.Security.Cryptography.MD5.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashCode = Convert.ToBase64String(md5.ComputeHash(contentBytes));
#endif
            postRequest.Headers[HttpRequestHeader.ContentMd5] = hashCode;

            _requestAuthenticator.Authenticate(postRequest);
            try
            {
                var ret = postRequest.GetResponse() as HttpWebResponse;
                if (ret != null)
                {
                    LastResponseTimestamp = DateTime.Now;
                }
                return ret;
            }
            catch (WebException wex)
            {
                var webExceptionDetail = GetAndLogWebExceptionDetail("POST", relativePath, wex);
                throw new BrightstarClientException(webExceptionDetail, wex);
            }
        }

        private HttpWebResponse AuthenticatedFormPost(string relativePath, IEnumerable<Tuple<string, string>> postBodyParameters, string acceptType)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var postRequest = WebRequest.Create(uri) as HttpWebRequest;
            if (postRequest == null) throw new ArgumentException(Strings.NotAnHttpRequest);
            postRequest.Method = "POST";
            SetDateHeader(postRequest);
            postRequest.Accept = acceptType;
            postRequest.ContentType = UrlEncodedFormContentType;
            var contentBuilder = new StringBuilder();
            foreach (var bodyParam in postBodyParameters)
            {
                contentBuilder.AppendFormat("{0}={1}", EscapeDataString(bodyParam.Item1),
                                            EscapeDataString(bodyParam.Item2));
                contentBuilder.Append("&");
            }
            var content = contentBuilder.ToString().TrimEnd('&');
            using (var writer = new StreamWriter(postRequest.GetRequestStream()))
            {
                writer.Write(content);
            }
#if PORTABLE
            var md5 = MD5.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashCode = Convert.ToBase64String(md5.ComputeHash(contentBytes));
#else
            var md5 = System.Security.Cryptography.MD5.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashCode = Convert.ToBase64String(md5.ComputeHash(contentBytes));
#endif
            postRequest.Headers[HttpRequestHeader.ContentMd5] = hashCode;
            //postRequest.ContentLength = contentBytes.Length;

            _requestAuthenticator.Authenticate(postRequest);
            try
            {
                var ret = postRequest.GetResponse() as HttpWebResponse;
                if (ret != null)
                {
                    LastResponseTimestamp = DateTime.Now;
                }
                return ret;
            }
            catch (WebException wex)
            {
                var webExceptionDetail = GetAndLogWebExceptionDetail("POST", relativePath, wex);
                throw new BrightstarClientException(webExceptionDetail, wex);
            }
        }

        private HttpWebResponse AuthenticatedDelete(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var deleteRequest = WebRequest.Create(uri) as HttpWebRequest;
            if (deleteRequest == null) throw new ArgumentException("Request does not use HTTP(S)");
            deleteRequest.Method = "DELETE";
            deleteRequest.Accept = JsonContentType;
            _requestAuthenticator.Authenticate(deleteRequest);
            try
            {
                var response = deleteRequest.GetResponse() as HttpWebResponse;
                if (response != null) LastResponseTimestamp = DateTime.Now;
                return response;
            }
            catch (WebException wex)
            {
                var webExceptionDetail = GetAndLogWebExceptionDetail("DELETE", relativePath, wex);
                throw new BrightstarClientException(webExceptionDetail, wex);                
            }
        }

        private static String GetAndLogWebExceptionDetail(string httpMethod, string requestUri, WebException wex)
        {
            String webExceptionDetail;

            if (wex.Response is HttpWebResponse)
            {                
                    var httpResponse = wex.Response as HttpWebResponse;
                    var responseStream = wex.Response.GetResponseStream();
                    if (responseStream != null)
                    {
                        using (var rdr = new StreamReader(responseStream))
                        {
                            var responseContent = rdr.ReadToEnd();

                            webExceptionDetail = String.Format("HTTP {0} to {1} failed. Server response was: {2} - {3} : {4}",
                                               httpMethod, requestUri, httpResponse.StatusCode,
                                               httpResponse.StatusDescription,
                                               responseContent);
                        }
                    }
                    else
                    {
                        webExceptionDetail = String.Format("HTTP {0} to {1} failed. Server response was: {2} - {3}",
                                           httpMethod, requestUri, httpResponse.StatusCode,
                                           httpResponse.StatusDescription);                        
                    }                                    
            }
            else
            {
                webExceptionDetail = String.Format("HTTP {0} to {1} failed. Could not process server response.",
                    httpMethod, requestUri);                
            }

            Logging.LogWarning(BrightstarEventId.TransportError, webExceptionDetail);
            return webExceptionDetail;
        }

        private static readonly byte[] Mark = new byte[] { (byte)'-', (byte)'_', (byte)'.', (byte)'~' };

        private static string EscapeDataString(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var escapeBuilder = new StringBuilder();
            var bytes = Encoding.UTF8.GetBytes(value);
            foreach (var octet in bytes)
            {

                if ((octet > 47 && octet < 58) ||
                    (octet > 64 && octet < 91) ||
                    (octet > 96 && octet < 123) ||
                    Mark.Contains(octet))
                {
                    escapeBuilder.Append((char)octet);
                }
                else
                {
                    escapeBuilder.AppendFormat("%{0:X2}", octet);
                }
            }
            return escapeBuilder.ToString();
        }

        private string CreateJob(string storeName, JobRequestObject jobRequest)
        {
            var response = AuthenticatedPost(storeName + "/jobs", jobRequest);
            if (response.StatusCode == HttpStatusCode.Created)
            {
#if PORTABLE || WINDOWS_PHONE
                return response.Headers["Location"];
#else
                return response.Headers[HttpResponseHeader.Location];
#endif
            }
            throw new BrightstarClientException(
                String.Format("Creation of job failed. Server returned status : {0} - {1}", response.StatusCode,
                              response.StatusDescription));
        }

        private static T Deserialize<T>(HttpWebResponse response)
        {
            string jsonString;
            if (response == null) throw new ArgumentNullException("response");
            var responseStream = response.GetResponseStream();
            if (responseStream == null) throw new BrightstarClientException("No body found in response. Expected a " + typeof(T).Name);
            using (var rdr = new StreamReader(responseStream))
            {
                jsonString = rdr.ReadToEnd();
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonString);
            //var ser = new JavaScriptSerializer();
            //return ser.Deserialize<T>(jsonString);
        }

#if !PORTABLE
        private IJobInfo PollJob(string jobUri)
        {
            var timer = new Stopwatch();
            timer.Start();
            while (true)
            {
                var jobInfoRepsonse = AuthenticatedGet(jobUri);
                var jobInfo = Deserialize<JobResponseModel>(jobInfoRepsonse);
                if (jobInfo.JobCompletedOk || jobInfo.JobCompletedWithErrors) return jobInfo;
                if (PollTimeout > 0 && timer.ElapsedMilliseconds > PollTimeout) break;
                Thread.Sleep(PollInterval);
            }
            throw new TimeoutException("Poll timeout exceeded");
        }
#endif
    }
}
