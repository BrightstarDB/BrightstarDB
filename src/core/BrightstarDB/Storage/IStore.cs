using System;
using System.Collections.Generic;
using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Model;
using BrightstarDB.Profiling;
using BrightstarDB.Query;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// This interface is the low level store interface.
    /// </summary>
    internal interface IStore : IDisposable
    {
        /// <summary>
        /// Get the full path to the store directory
        /// </summary>
        string DirectoryPath { get; }

        // match
        IEnumerable<Triple> Match(string subject, string predicate, string obj, bool isLiteral = false, string dataType = null, string langCode = null, string graph = null);
        IEnumerable<Triple> Match(string subject, string predicate, string obj, bool isLiteral = false, string dataType = null, string langCode = null, IEnumerable<string> graphs = null);

        // get proxy statements
        IEnumerable<Triple> GetResourceStatements(string resource, string graphUri = null);

        // query
        void ExecuteSparqlQuery(string exp, SparqlResultsFormat resultsFormat, Stream resultStream, out BrightstarSparqlResultsType resultsType);

        /// <summary>
        /// Execute a SPARQL query against this store
        /// </summary>
        /// <param name="exp">The SPARQL query expression</param>
        /// <param name="resultsFormat">The requested SPARQL results format</param>
        /// <param name="defaultGraphUris">OPTIONAL: An enumeration of the URIs of the graphs to be treated as the default graph in the SPARQL dataset</param>
        /// <returns>The SPARQL query results in the requested format</returns>
        string ExecuteSparqlQuery(string exp, SparqlResultsFormat resultsFormat, IEnumerable<string> defaultGraphUris = null);

        /// <summary>
        /// Overload of ExecuteSparqlQuery that returns a count of the number of rows returned by the query
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="resultsFormat"></param>
        /// <param name="rowCount"></param>
        /// <returns></returns>
        string ExecuteSparqlQuery(string expression, SparqlResultsFormat resultsFormat, out long rowCount);

        /// <summary>
        /// Insert a triple into the store
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="predicate"></param>
        /// <param name="objValue"></param>
        /// <param name="isObjectLiteral"></param>
        /// <param name="dataType"></param>
        /// <param name="langCode"></param>
        /// <param name="graphUri"></param>
        /// <param name="profiler"></param>
        void InsertTriple(string subject, string predicate, string objValue, bool isObjectLiteral, string dataType, string langCode, string graphUri, BrightstarProfiler profiler = null);

        /// <summary>
        /// Inserts a triple into the store
        /// </summary>
        /// <param name="triple"></param>
        void InsertTriple(Triple triple);

        /// <summary>
        /// Delete triple from store
        /// </summary>
        /// <param name="triple"></param>
        void DeleteTriple(Triple triple);

        // commit changes
        void Commit(Guid jobId, BrightstarProfiler profiler = null);

        /// <summary>
        /// Commits all updates so far to the store and returns the start position of the Store object in the store file.
        /// </summary>
        void FlushChanges(BrightstarProfiler profiler= null);

        /// <summary>
        /// Returns a list of commit points with the most recent returned first
        /// </summary>
        /// <returns>Store commit points, most recent first.</returns>
        IEnumerable<CommitPoint> GetCommitPoints();

        /// <summary>
        /// Makes the provided commit point the most recent one.
        /// </summary>
        /// <param name="commitPoint">The commitpoint to make the most recent.</param>
        void RevertToCommitPoint(CommitPoint commitPoint);

        /// <summary>
        /// Get an enumeration over all graph URIs in the store
        /// </summary>
        /// <returns>An enumeration of string values where each value is the URI of a graph that was previously added to the store</returns>
        IEnumerable<string> GetGraphUris();

        /// <summary>
        /// Removes all unused data.
        /// </summary>
        /// <param name="jobId"></param>
        void Consolidate(Guid jobId);

        /// <summary>
        /// Copies all the indexes from this store to the specified target page store
        /// </summary>
        /// <param name="pageStore">The page store to copy to</param>
        /// <param name="txnId">The transaction Id to use in the target page store for the write</param>
        /// <returns>The ID of the root store page created in the target page store</returns>
        ulong CopyTo(IPageStore pageStore, ulong txnId);

        /// <summary>
        /// Clear out any existing data in the target graph, then copy all triples from the source graph to the target graph
        /// </summary>
        /// <param name="srcGraphUri"></param>
        /// <param name="targetGraphUri"></param>
        void CopyGraph(string srcGraphUri, string targetGraphUri);

        /// <summary>
        /// Remove all data from target graph, copy all triples from source graph to target graph, delete all data from source graph
        /// </summary>
        /// <param name="srcGraphUri"></param>
        /// <param name="targetGraphUri"></param>
        void MoveGraph(string srcGraphUri, string targetGraphUri);

        /// <summary>
        /// Copy all triples from source graph to target graph
        /// </summary>
        /// <param name="srcGraphUri"></param>
        /// <param name="targetGraphUri"></param>
        void AddGraph(string srcGraphUri, string targetGraphUri);
        void DeleteGraph(string graphUri);
        void DeleteGraphs(IEnumerable<string> graphUris);
        void Close();

        /// <summary>
        /// Returns an enumeration over the unique predicate URIs in the store
        /// </summary>
        /// <param name="profiler"></param>
        /// <returns></returns>
        IEnumerable<string> GetPredicates(BrightstarProfiler profiler = null);

        /// <summary>
        /// Counts the total number of triples with the specified predicate in the store.
        /// </summary>
        /// <param name="predicateUri">The predicate URI to count triples for</param>
        /// <param name="profiler"></param>
        /// <returns>The number of triples matching the specified predicate</returns>
        ulong GetTripleCount(string predicateUri, BrightstarProfiler profiler = null);
    }
}
