using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// .NET wrapper for the Brightstar REST API
    /// </summary>
    public class BrightstarRestClient : IBrightstarService
    {
        private Uri _serviceEndpoint;
        private string _accountId;
        private string _authKey;
        private const string JsonContentType = "application/json";
        //private const string XmlContentType = "application/xml";
        private const string UrlEncodedFormContentType = "application/x-www-form-urlencoded";

        internal BrightstarRestClient(string serviceEndpoint, string accountId, string authenticationKey)
        {
            _serviceEndpoint = serviceEndpoint.EndsWith("/") ? new Uri(serviceEndpoint) : new Uri(serviceEndpoint + "/");
            _accountId = accountId;
            _authKey = authenticationKey;
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
            return Deserialize<string[]>(response);
        }

        /// <summary>
        /// Create a new store
        /// </summary>
        /// <param name="storeName">The name of the store to be created</param>
        public void CreateStore(string storeName)
        {
            var response = AuthenticatedPost("",
                                             new List<Tuple<string, string>>
                                                 {
                                                     new Tuple<string, string>("storeName", storeName)
                                                 });
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new BrightstarClientException(
                    String.Format("Store not created. Server response was: {0} - {1}", response.StatusCode,
                                  response.StatusDescription));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="persistenceType"></param>
        public void CreateStore(string storeName, PersistenceType persistenceType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public void DeleteStore(string storeName)
        {
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
            try
            {
                AuthenticatedHead(storeName);
                return true;
            }
            catch (WebException wex)
            {
                if (wex.Status == WebExceptionStatus.ProtocolError &&
                    (wex.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
                throw;
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
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression,
                                   DateTime? ifNotModifiedSince = new DateTime?(),
                                   SparqlResultsFormat resultsFormat = null)
        {
            return ExecuteQuery(storeName, queryExpression, (string[]) null, ifNotModifiedSince, resultsFormat);
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
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression,
                                   string defaultGraphUri,
                                   DateTime? ifNotModifiedSince = new DateTime?(),
                                   SparqlResultsFormat resultsFormat = null)
        {
            return ExecuteQuery(storeName, queryExpression, new string[] {defaultGraphUri}, ifNotModifiedSince,
                                resultsFormat);
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
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression,
            IEnumerable<string> defaultGraphUris,
            DateTime? ifNotModifiedSince = new DateTime?(),
            SparqlResultsFormat resultsFormat = null)
        {
            if (resultsFormat == null) resultsFormat = SparqlResultsFormat.Xml;
            // TODO: Add media type to request header
            if (ifNotModifiedSince.HasValue)
            {
                var headResponse = AuthenticatedHead(storeName);
                if (headResponse.LastModified <= ifNotModifiedSince.Value)
                {
                    throw new BrightstarClientException("Store not modified");
                }
            }
            var parameters = new List<Tuple<string, string>> {new Tuple<string, string>("query", queryExpression)};
            if (defaultGraphUris != null)
            {
                parameters.AddRange(defaultGraphUris.Select(g => new Tuple<string, string>("default-graph-uri", g)));
            }
            
            var queryResponse = AuthenticatedPost(storeName, parameters);
            return queryResponse.GetResponseStream();
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression,
                                   SparqlResultsFormat resultsFormat)
        {
            return ExecuteQuery(commitPoint, queryExpression, (IEnumerable<string>) null, resultsFormat);
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="defaultGraphUri">The URI of the default graph for the query</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression, string defaultGraphUri,
                                   SparqlResultsFormat resultsFormat)
        {
            return ExecuteQuery(commitPoint, queryExpression, new string[] {defaultGraphUri}, resultsFormat);
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="defaultGraphUris"></param>
        /// <param name="resultsFormat"> </param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression, IEnumerable<string> defaultGraphUris, SparqlResultsFormat resultsFormat)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions">NTriples that must be in the store in order for the transaction to execute</param>
        /// <param name="deletePatterns">The delete patterns that will be removed from the store</param>
        /// <param name="insertData">The NTriples data that will be inserted into the store.</param>
        /// <param name="defaultGraphUri">The URI of the default graph to be updated by the transaction.</param>
        /// <param name="waitForCompletion">If set to true the method will block until the transaction completes</param>
        /// <returns>Job Info</returns>
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns,
                                           string insertData, string defaultGraphUri, bool waitForCompletion = true)
        {
            string jobData = "GRAPH " + defaultGraphUri + "\n" + String.Join("\n.\n", preconditions, deletePatterns, insertData);
            var jobUri = CreateJob(storeName, "Transaction", jobData);
            if (waitForCompletion)
            {
                return PollJob(jobUri, 500, 0);
            }
            else
            {
                var jobInfoResponse = AuthenticatedGet(jobUri);
                return Deserialize<RestJobInfo>(jobInfoResponse);
            }
        }

        /// <summary>
        /// Execute a SPARQL Update expression against a store
        /// </summary>
        /// <param name="storeName">The name of the store to be updated</param>
        /// <param name="updateExpression">The SPARQL Update expression to be applied</param>
        /// <param name="waitForCompletion">If set to true, the method will block until the transaction completes</param>
        /// <returns>A <see cref="JobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteUpdate(string storeName, string updateExpression, bool waitForCompletion = true)
        {
            // TODO: Implement update with SPARQL protocol
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the information about a job. Including status and any messages.
        /// </summary>
        /// <param name="storeName">Name of the store where the job is running</param>
        /// <param name="jobId">The Id of the job</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo GetJobInfo(string storeName, string jobId)
        {
            var jobInfoResponse = AuthenticatedGet(storeName + "/jobs/" + jobId);
            return Deserialize<RestJobInfo>(jobInfoResponse);
        }

        /// <summary>
        /// Starts an import job.
        /// </summary>
        /// <param name="store">The store to perform the import to</param>
        /// <param name="fileName">The URI of the data to import.</param>
        /// <returns>A JobInfo instance</returns>
        /// <remarks>This method starts a job on the server to import data from an NTriples format
        /// RDF data source. 
        /// For control over Azure blob storage credentials and compression, use the overloaded version of this method</remarks>
        public IJobInfo StartImport(string store, string fileName)
        {
            var importSource = new BlobImportSource {BlobUri = fileName};
            return StartImport(store, importSource);
        }

        /// <summary>
        /// Starts an import job
        /// </summary>
        /// <param name="store">The store to import into</param>
        /// <param name="fileName">The URI of the data to import</param>
        /// <param name="graphUri">The URI identifier of the graph that the data is to be imported into. If NULL, import is into the default graph</param>
        /// <returns>A <see cref="IJobInfo"/> instance to use for monitoring the progress of the job</returns>
        public IJobInfo StartImport(string store, string fileName, string graphUri)
        {
            var importSource = new BlobImportSource { BlobUri = fileName, Graph = graphUri};
            return StartImport(store, importSource);
        }

        private IJobInfo StartImport(string store, BlobImportSource importSource)
        {
        var jobUri = CreateJob(store, "Import", importSource.ToJsonString());
            var jobInfoResponse = AuthenticatedGet(jobUri);
            return Deserialize<RestJobInfo>(jobInfoResponse);
        }

        /// <summary>
        /// Starts an import job with the option to read from a compressed data source
        /// </summary>
        /// <param name="store">The store to import into</param>
        /// <param name="sourceUri">The URI of the data to be imported</param>
        /// <param name="useGZip">Flag indicating if the source data is compressed</param>
        /// <returns>A <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo StartImport(string store, string sourceUri, bool useGZip )
        {
            return StartImport(store, new BlobImportSource {BlobUri = sourceUri, IsGZiped = useGZip});
        }

        /// <summary>
        /// Starts an import job reading data from Azure blob storage
        /// </summary>
        /// <param name="store">The store to import into</param>
        /// <param name="blobStoreConnectionString">The connection string to the Azure blob storage</param>
        /// <param name="blobAddress">The URI of the data to be imported</param>
        /// <param name="useGZip">Flag indicating if the source data is compressed</param>
        /// <returns>A <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo StartImport(string store, string blobStoreConnectionString, string blobAddress, bool useGZip)
        {
            return StartImport(store, new BlobImportSource { ConnectionString = blobStoreConnectionString, BlobUri = blobAddress, IsGZiped = useGZip });
        }

        /// <summary>
        /// Starts an export job
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="fileName">The name of the file in the brightstar\import folder to write to. This file will be overwritten if it already exists.</param>
        /// <param name="graphUri">The identifier of the store graph to be exported. If NULL, all graphs in the store will be exported.</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartExport(string store, string fileName, string graphUri)
        {
            return StartExport(store, new BlobImportSource {BlobUri = fileName, Graph = graphUri});
        }

        /// <summary>
        /// Starts an export job with the option to compress the exported data
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="targetUri">The URI to write the data to</param>
        /// <param name="useGZip">Flag indicating if the exported data should be compressed as it is exported</param>
        /// <returns>A <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo StartExport(string store, string targetUri, bool useGZip)
        {
            return StartExport(store, new BlobImportSource {BlobUri = targetUri, IsGZiped = useGZip});
        }

        /// <summary>
        /// Starts an export job to write to an Azure blob
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="blobStoreConnectionString">The connection string to the Azure blob storage</param>
        /// <param name="blobAddress">The URI of the blob to write to</param>
        /// <param name="useGZip">Flag indicating if the exported data should be compressed as it is exported</param>
        /// <returns>A <see cref="IJobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo StartExport(string store, string blobStoreConnectionString, string blobAddress, bool useGZip)
        {
            return StartExport(store,
                               new BlobImportSource
                                   {
                                       ConnectionString = blobStoreConnectionString,
                                       BlobUri = blobAddress,
                                       IsGZiped = useGZip
                                   });
        }

        private IJobInfo StartExport(string store, BlobImportSource exportSource)
        {
            var jobUri = CreateJob(store, "Export", exportSource.ToJsonString());
            var jobInfoResponse = AuthenticatedGet(jobUri);
            return Deserialize<RestJobInfo>(jobInfoResponse);
        }

        /// <summary>
        /// Creates a new store file containing only the data required for the current state
        /// </summary>
        /// <param name="store">The store to consolidate</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo ConsolidateStore(string store)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// This will make the commit point provided the new latest commit point. Blocks until the operation is complete.
        /// </summary>
        /// <param name="storeName">The name of the store to be reverted</param>
        /// <param name="commitPoint">The commit point to revert to</param>
        public void RevertToCommitPoint(string storeName, ICommitPointInfo commitPoint)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes a previous transaction
        /// </summary>
        /// <param name="storeName">The name of the store to re-apply the transaction to</param>
        /// <param name="transactionInfo">The transaction to be applied</param>
        public IJobInfo ReExecuteTransaction(string storeName, ITransactionInfo transactionInfo)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the most recent statistics for the specified store
        /// </summary>
        /// <param name="storeName">The name of the store to retrieve statistics for.</param>
        /// <returns>A <see cref="IStoreStatistics"/> instance containing the most recent statistics for the named store, or NULL if
        /// there are no statistics availabe for the store.</returns>
        public IStoreStatistics GetStatistics(string storeName)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queues a job to update the statistics for a store
        /// </summary>
        /// <param name="storeName">The name of the store whose statistics are to be updated</param>
        /// <returns>A <see cref="IJobInfo"/> instance for tracking the current status of the job.</returns>
        public IJobInfo UpdateStatistics(string storeName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queues a job to create a snapshot of a store
        /// </summary>
        /// <param name="storeName">The name of the store to take a snapshot of</param>
        /// <param name="targetStoreName">The name of the store to be created to receive the snapshot</param>
        /// <param name="persistenceType">The type of persistence to use for the target store</param>
        /// <param name="sourceCommitPoint">OPTIONAL: the commit point in the source store to take a snapshot from</param>
        /// <returns>A <see cref="IJobInfo"/> instance for tracking the current status of the job.</returns>
        public IJobInfo CreateSnapshot(string storeName, string targetStoreName, PersistenceType persistenceType, ICommitPointInfo sourceCommitPoint = null)
        {
            throw new NotImplementedException();
        }

        #endregion


        private HttpWebResponse AuthenticatedGet(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var getRequest = WebRequest.Create(uri.ToString()) as HttpWebRequest;
            getRequest.ContentType = JsonContentType;
            getRequest.Date = DateTime.UtcNow;
            SignRequest(getRequest);
            var ret = getRequest.GetResponse() as HttpWebResponse;
            if (ret != null)
            {
                LastResponseTimestamp = DateTime.Now;
            }
            return ret;
        }

        private HttpWebResponse AuthenticatedHead(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var headRequest = WebRequest.Create(uri.ToString()) as HttpWebRequest;
            headRequest.Method = "HEAD";
            headRequest.Date = DateTime.UtcNow;
            SignRequest(headRequest);
            return headRequest.GetResponse() as HttpWebResponse;
        }

        private HttpWebResponse AuthenticatedPost(string relativePath, IEnumerable<Tuple<string, string>> postBodyParameters)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var postRequest = WebRequest.Create(uri) as HttpWebRequest;
            postRequest.Method = "POST";
            postRequest.Date = DateTime.UtcNow;
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
            var md5 = System.Security.Cryptography.MD5.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashCode = Convert.ToBase64String(md5.ComputeHash(contentBytes));
            postRequest.Headers[HttpRequestHeader.ContentMd5] = hashCode;
            //postRequest.ContentLength = contentBytes.Length;

            SignRequest(postRequest);
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
                string responseContent;
                if (wex.Response != null)
                {
                    using (var rdr = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        responseContent = rdr.ReadToEnd();
                        Logging.LogWarning(BrightstarEventId.TransportError,
                            "HTTP POST to {0} failed. Server response was: {1}",
                            relativePath, responseContent);
                    }
                }
                throw;
            }
        }

        private static readonly byte[] Mark = new byte[] { (byte)'-', (byte)'_', (byte)'.', (byte)'~' };

        private static String EscapeDataString(string value)
        {
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

        private string CreateJob(string storeName, string jobType, string jobData)
        {
            var postBodyParameters = new List<Tuple<string, string>>
                                         {
                                             new Tuple<string, string>("jobType", jobType),
                                             new Tuple<string, string>("jobData", jobData)
                                         };
            var response = AuthenticatedPost(storeName + "/jobs", postBodyParameters);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                return response.Headers[HttpResponseHeader.Location];
            }
            throw new BrightstarClientException(
                String.Format("Creation of job failed. Server returned status : {0} - {1}", response.StatusCode,
                              response.StatusDescription));
        }

        private HttpWebResponse AuthenticatedDelete(string relativePath)
        {
            var uri = new Uri(_serviceEndpoint, relativePath);
            var deleteRequest = WebRequest.Create(uri) as HttpWebRequest;
            SignRequest(deleteRequest);
            deleteRequest.Method = "DELETE";
            var response = deleteRequest.GetResponse() as HttpWebResponse;
            if (response != null) LastResponseTimestamp = DateTime.Now;
            return response;
        }
        /// <summary>
        /// Adds the required authentication and timestamp headers to the request
        /// </summary>
        /// <param name="request"></param>
        private void SignRequest(HttpWebRequest request)
        {
           request.Headers.Add(HttpRequestHeader.Authorization,
               "SharedKey " + _accountId + ":" + RestClientHelper.GenerateSignature(request, SignatureType.SharedKey,  _authKey));
        }

        
        private T Deserialize<T>(HttpWebResponse response)
        {
            string jsonString;
            using (var rdr = new StreamReader(response.GetResponseStream()))
            {
                jsonString = rdr.ReadToEnd();
            }
            var ser = new JavaScriptSerializer();
            return ser.Deserialize<T>(jsonString);
        }

        private IJobInfo PollJob(string jobUri, int pollInterval, int pollTimeout)
        {
            var timer = new Stopwatch();
            timer.Start();
            while (true)
            {
                var jobInfoRepsonse = AuthenticatedGet(jobUri);
                var jobInfo = Deserialize<RestJobInfo>(jobInfoRepsonse);
                if (jobInfo.JobCompletedOk || jobInfo.JobCompletedWithErrors) return jobInfo;
                if (pollTimeout > 0 && timer.ElapsedMilliseconds > pollTimeout) break;
                Thread.Sleep(pollInterval);
            }
            throw new TimeoutException("Poll timeout exceeded");
        }
    }
}
