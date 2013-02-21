using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using BrightstarDB.Storage;

namespace BrightstarDB.Service
{
    [ServiceContract(Namespace = "http://brightstardb.com/services/core/", Name="IBrightstarWcfService")]
    public interface IBrightstarService
    {
        /// <summary>
        /// List the names of the stores managed by this brightstar instance
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IEnumerable<string> ListStores();

        /// <summary>
        /// Create a new store
        /// </summary>
        /// <param name="storeName"></param>
        [OperationContract]
        void CreateStore(string storeName);

        /// <summary>
        /// Create a new store with a specific persistence type
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="persistenceType"></param>
        [OperationContract]
        void CreateStoreWithPersistenceType(string storeName, PersistenceType persistenceType);

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName"></param>
        [OperationContract]
        void DeleteStore(string storeName);

        /// <summary>
        /// Checks to see if the named store already exists
        /// </summary>
        /// <param name="storeName">store to check</param>
        /// <returns>true if store exists, false if not.</returns>
        [OperationContract]
        bool DoesStoreExist(string storeName);

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">Store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="ifNotModifiedSince">If the store has not been modified since the date/time specified by this parameter, 
        /// a StoreNotModified exception will be raised.</param>
        /// <param name="resultsMediaType">OPTIONAL: The media type to use for serializing the results</param>
        /// <returns>A stream containing the serialized query results</returns>
        [OperationContract]
        Stream ExecuteQuery(string storeName, string queryExpression, DateTime? ifNotModifiedSince, string resultsMediaType = null);

        /// <summary>
        /// Queries a given commit point of a store using a SPARQL query
        /// </summary>
        /// <param name="commitPoint">The store commit point to be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="resultsMediaType">OPTIONAL: The media type to use for serializing the results</param>
        /// <returns>A stream containing the serialized query results</returns>
        [OperationContract]
        Stream ExecuteQueryOnCommitPoint(CommitPointInfo commitPoint, string queryExpression, string resultsMediaType = null);

        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions"></param>
        /// <param name="deletePatterns"></param>
        /// <param name="insertData"></param>
        /// <returns>Job id</returns>
        [OperationContract]
        JobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns, string insertData);

        /// <summary>
        /// Returns all the triples in the named store
        /// </summary>
        /// <param name="storeName">Store to retrieve triples from</param>
        /// <returns>A stream containing triples encoded using NTriples format</returns>
        //[OperationContract]
        //Stream GetStoreData(string storeName);

        /// <summary>
        /// Gets the information about a job. Including status and any messages.
        /// </summary>
        /// <param name="storeName">Name of the store where the job is running</param>
        /// <param name="jobId">The Id of the job</param>
        /// <returns>A JobInfo instance</returns>
        [OperationContract]
        JobInfo GetJobInfo(string storeName, string jobId);

        /// <summary>
        /// Starts an import job.
        /// </summary>
        /// <param name="store">The store to perform the import to</param>
        /// <param name="fileName">The name of the file in brighhtstar\import folder to import.</param>
        /// <param name="graphUri">The URI of the graph to import data into.</param>
        /// <returns>A JobInfo instance</returns>
        [OperationContract]
        JobInfo StartImport(string store, string fileName, string graphUri = Constants.DefaultGraphUri);

        /// <summary>
        /// Starts an export job.
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="fileName">The name of the file in the brightstar\import folder to write to. If this file exists, it will be overwritten.</param>
        /// <param name="graphUri">The URI of the graph to be exported. If NULL, all graphs are exported.</param>
        /// <returns>A JobInfo instance to monitor the status of the job</returns>
        [OperationContract]
        JobInfo StartExport(string store, string fileName, string graphUri = null);

        /// <summary>
        /// Starts a SPARQL Update job
        /// </summary>
        /// <param name="store">The store to be updated</param>
        /// <param name="updateExpression">A SPARQL Update expression</param>
        /// <returns>A JobInfo instance to monitor the status of the job</returns>
        [OperationContract]
        JobInfo ExecuteUpdate(string store, string updateExpression);

        /// <summary>
        /// Creates a new store file containing only the data required for the current state
        /// </summary>
        /// <param name="store">The store to consolidate</param>
        /// <returns>A JobInfo instance</returns>
        [OperationContract]
        JobInfo ConsolidateStore(string store);

        /// <summary>
        /// returns commit points in batches 
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="skip">How many commit points to skip over</param>
        /// <param name="take">Max allowed value is 100</param>
        /// <returns></returns>
        [OperationContract]
        IEnumerable<CommitPointInfo> GetCommitPoints(string storeName, int skip, int take);

        /// <summary>
        /// Returns a batch of commit points that lie within a given date/time range
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="latest">The latest commit date of the commit points to return</param>
        /// <param name="earliest">The earliest commit date of the commit points to return</param>
        /// <param name="skip">The offset into the results list</param>
        /// <param name="take">The number of results to return. Maximum allows is 100</param>
        /// <returns></returns>
        [OperationContract]
        IEnumerable<CommitPointInfo> GetCommitPointsInDateRange(string storeName, DateTime latest, DateTime earliest,
                                                                int skip, int take);

        /// <summary>
        /// Returns the commit point that was effective at the specified date/time
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="timestamp">The date/time timestamp</param>
        /// <returns>The commit point that was effective at <paramref name="timestamp"/> or null if 
        /// the store was created after <paramref name="timestamp"/> or if the store history has been subsequently removed by a coalesce operation.</returns>
        [OperationContract]
        CommitPointInfo GetCommitPoint(string storeName, DateTime timestamp);

        /// <summary>
        /// This will make the commit point provided the new latest commit point. Blocks until the operation is complete.
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="commitPoint"></param>
        [OperationContract]
        void RevertToCommitPoint(string storeName, CommitPointInfo commitPoint);

        /// <summary>
        /// Gets a list of transations
        /// </summary>
        /// <param name="storeName">Name of store for </param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [OperationContract]
        IEnumerable<TransactionInfo> GetTransactions(string storeName, int skip, int take);

        /// <summary>
        /// Executes a previous transaction
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="transactionInfo"></param>
        [OperationContract]
        JobInfo ReExecuteTransaction(string storeName, TransactionInfo transactionInfo);        
    }
}
