using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BrightstarDB.Client;
using BrightstarDB.Storage;
using ITransactionInfo = BrightstarDB.Client.ITransactionInfo;

namespace BrightstarDB.Azure.Gateway
{
    internal class BrightstarClusterClient : IBrightstarService
    {

        private DateTime? _lastResponse;

        public BrightstarClusterClient()
        {
            _lastResponse = null;
        }

        #region Implementation of IBrightstarService

        /// <summary>
        /// Returns the timestamp provided by the server on its last response.
        /// </summary>
        /// <remarks>This property will be null if no operation has been invoked, or 
        /// if the client is an embedded client.</remarks>
        public DateTime? LastResponseTimestamp
        {
            get { return _lastResponse; }
        }

        /// <summary>
        /// List the names of the stores managed by this Brightstar server
        /// </summary>
        /// <returns>An enumeration over the names of the stores managed by the Brightstar server</returns>
        public IEnumerable<string> ListStores()
        {
            return BrightstarCluster.Instance.GetStores();
        }

        /// <summary>
        /// Create a new store
        /// </summary>
        /// <param name="storeName">The name of the store to be created</param>
        public void CreateStore(string storeName)
        {
            BrightstarCluster.Instance.CreateStore(storeName);
        }

        /// <summary>
        /// Create a new store with a specific persistence type for the main store indexes
        /// </summary>
        /// <param name="storeName">The name of the store to be created</param>
        /// <param name="persistenceType">The type of persistence to use for the main store indexes</param>
        public void CreateStore(string storeName, PersistenceType persistenceType)
        {
            if (persistenceType == PersistenceType.Rewrite)
            {
                throw new NotSupportedException("The rewrite store type is not currently supported by BrightstarDB on Azure");
            }
            BrightstarCluster.Instance.CreateStore(storeName);
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public void DeleteStore(string storeName)
        {
            BrightstarCluster.Instance.DeleteStore(storeName);
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
                BrightstarCluster.Instance.GetLastModifiedDate(storeName);
                return true;
            }
            catch (StoreNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a BrightstarClientException will be raised with the message "Store not modified".</param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression, DateTime? ifNotModifiedSince = new DateTime?())
        {
            var result = BrightstarCluster.Instance.ExecuteQuery(storeName, queryExpression);
            return new MemoryStream(System.Text.Encoding.Unicode.GetBytes(result));
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression)
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
        /// <param name="waitForCompletion">If set to true the method will block until the transaction completes</param>
        /// <returns>Job Info</returns>
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns, string insertData, bool waitForCompletion = true)
        {
            var jobId = BrightstarCluster.Instance.StartUpdateTransaction(storeName, preconditions, deletePatterns,
                                                                          insertData);
            while (waitForCompletion)
            {
                Thread.Sleep(500);
                var jobInfo = GetJobInfo(storeName, jobId);
                if (jobInfo.JobCompletedOk || jobInfo.JobCompletedWithErrors) return jobInfo;
            }
            return GetJobInfo(storeName, jobId);
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
            var jobInfo = BrightstarCluster.Instance.GetJobInfo(storeName, jobId);
            return new RestJobInfo
                       {Id = jobInfo.Id, Status = jobInfo.Status.ToString(), StatusMessage = jobInfo.StatusMessage};
        }

        /// <summary>
        /// Starts an import job.
        /// </summary>
        /// <param name="store">The store to perform the import to</param>
        /// <param name="fileName">The name of the file in brighhtstar\import folder to import.</param>
        /// <param name="graphUri">The URI identifier of the graph to import into. If NULL, the import is into the default graph</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartImport(string store, string fileName, string graphUri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts an export job
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="fileName">The name of the file in the brightstar\import folder to write to. This file will be overwritten if it already exists.</param>
        /// <param name="graphUri">The URI identifier of the graph to be exported. If NULL, all graphs in the store are exported</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartExport(string store, string fileName, string graphUri)
        {
            throw new NotImplementedException();
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
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, DateTime latest, DateTime earliest, int skip, int take)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}