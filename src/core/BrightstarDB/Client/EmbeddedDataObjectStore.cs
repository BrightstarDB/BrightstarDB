using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BrightstarDB.Dto;
using BrightstarDB.EntityFramework.Query;
using BrightstarDB.Model;
using BrightstarDB.Rdf;
using BrightstarDB.Server;

namespace BrightstarDB.Client
{
    internal class EmbeddedDataObjectStore : DataObjectStoreBase
    {
        private readonly ServerCore _serverCore;
        private readonly string _storeName;
        private readonly bool _optimisticLockingEnabled;

        internal EmbeddedDataObjectStore(ServerCore serverCore, string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled,
            string updateGraphUri, IEnumerable<string> datasetGraphUris, string versionGraphUri)
            : base(namespaceMappings, updateGraphUri ?? Constants.DefaultGraphUri, datasetGraphUris, versionGraphUri)
        {
            _serverCore = serverCore;
            _storeName = storeName;
            _optimisticLockingEnabled = optimisticLockingEnabled;
            ResetTransactionData();
        }

        #region Implementation of IDataObjectStore


        public override IDataObject GetDataObject(string identity)
        {
            if (identity == null) throw new ArgumentNullException("identity");
            var resolvedIdentity = ResolveIdentity(identity);

            DataObject registeredDataObject = RegisterDataObject(new DataObject(this, resolvedIdentity));
            if (!registeredDataObject.IsLoaded)
            {
                var triples = GetFilteredResourceStatements(_storeName, resolvedIdentity)
                    .Union(AddTriples.Where(p => p.Subject.Equals(resolvedIdentity)));
                registeredDataObject.BindTriples(triples);
            }
            return registeredDataObject;
        }

        
        public override IEnumerable<IDataObject> BindDataObjectsWithSparql(string sparqlExpression)
        {
            var helper = new SparqlResultDataObjectHelper(this);
            return helper.BindDataObjects(ExecuteSparql(new SparqlQueryContext(sparqlExpression)));
        }

        public override SparqlResult ExecuteSparql(SparqlQueryContext sparqlQueryContext)
        {
            var resultStream = new MemoryStream();
            _serverCore.Query(_storeName, sparqlQueryContext.SparqlQuery, DataSetGraphUris, null, SparqlResultsFormat.Xml,
                              RdfFormat.RdfXml, resultStream);
            resultStream.Seek(0, SeekOrigin.Begin);
            return new SparqlResult(resultStream, sparqlQueryContext);
        }

        public override bool BindDataObject(DataObject dataObject)
        {
            var triples = GetFilteredResourceStatements(_storeName, dataObject.Identity);
            return dataObject.BindTriples(triples);
        }

        protected override void Cleanup()
        {
            // Can't really close down the ServerCore here as it may be shared by
            // multiple context objects
            // Perhaps it would be an idea to implement some sort of reference counting ?
            //_serverCore.Shutdown(true);
        }

        /// <summary>
        /// Commits all changes. Waits for the operation to complete.
        /// </summary>
        protected override void DoSaveChanges()
        {
            if (_optimisticLockingEnabled)
            {
                // get subject entity and see if there is a version triple
                var subjects =
                    AddTriples.Select(x => x.Subject)
                              .Distinct()
                              .Union(DeletePatterns.Select(x => x.Subject).Distinct())
                              .Except(new[] {Constants.WildcardUri}).ToList();
                foreach (var subject in subjects)
                {
                    var entity = LookupDataObject(subject);
                    if (entity == null) throw new BrightstarClientException("No Entity Found for Subject " + subject);

                    var version = entity.GetPropertyValue(Constants.VersionPredicateUri);
                    if (version == null)
                    {
                        // no existing version information so assume this is the first time using it with 1
                        entity.SetProperty(Constants.VersionPredicateUri, 1);
                    }
                    else
                    {
                        var intVersion = Convert.ToInt32(version);
                        // inc version
                        intVersion++;
                        entity.SetProperty(Constants.VersionPredicateUri, intVersion);
                        Preconditions.Add(new Triple
                                                {
                                                    Graph = VersionGraphUri,
                                                    DataType = RdfDatatypes.Integer,
                                                    IsLiteral = true,
                                                    LangCode = null,
                                                    Object = version.ToString(),
                                                    Predicate = Constants.VersionPredicateUri,
                                                    Subject = subject
                                                });
                    }
                }
            }

            var deleteData = new StringWriter();
            var dw = new BrightstarTripleSinkAdapter(new NQuadsWriter(deleteData, UpdateGraphUri));
            foreach (var triple in DeletePatterns)
            {
                dw.Triple(triple);
            }
            deleteData.Close();

            var addData = new StringWriter();
            var aw = new BrightstarTripleSinkAdapter(new NQuadsWriter(addData, UpdateGraphUri));
            foreach (var triple in AddTriples)
            {
                aw.Triple(triple);               
            }
            addData.Close();

            var preconditionsData = new StringWriter();
            var pw = new BrightstarTripleSinkAdapter(new NQuadsWriter(preconditionsData, UpdateGraphUri));
            foreach (var triple in Preconditions)
            {
                pw.Triple(triple);
            }
            preconditionsData.Close();

            var jobId = _serverCore.ProcessTransaction(_storeName, preconditionsData.ToString(), deleteData.ToString(), addData.ToString(), UpdateGraphUri);
            var status = _serverCore.GetJobStatus(_storeName, jobId.ToString());
            while (!(status.JobStatus == JobStatus.CompletedOk || status.JobStatus == JobStatus.TransactionError))
            {
                // wait for completion.
#if !PORTABLE
                Thread.Sleep(5);
#endif
                status = _serverCore.GetJobStatus(_storeName, jobId.ToString());
            }

            if (status.JobStatus == JobStatus.TransactionError)
            {
                if (status.ExceptionDetail.Type.Equals(typeof(PreconditionFailedException).FullName))
                {
                    var failedTriples =
                        status.ExceptionDetail.Message.Substring(status.ExceptionDetail.Message.IndexOf("\n") + 1);
                    Preconditions.Clear();
                    throw new TransactionPreconditionsFailedException(failedTriples);
                }
                throw new BrightstarClientException(status.ExceptionDetail != null  && !String.IsNullOrEmpty(status.ExceptionDetail.Message) ? status.ExceptionDetail.Message : "The transaction encountered an error");
            }

            // reset changes
            ResetTransactionData();
        }

        #endregion

        private IEnumerable<Triple> GetFilteredResourceStatements(string storeId, string resourceUri)
        {
            if (DataSetGraphUris == null)
            {
                return _serverCore.GetResourceStatements(storeId, resourceUri);
            }
            else
            {
                return _serverCore.GetResourceStatements(storeId, resourceUri)
                                  .Where(t =>
                                         DataSetGraphUris.Contains(t.Graph) ||
                                         (_optimisticLockingEnabled && t.Predicate.Equals(Constants.VersionPredicateUri) &&
                                          t.Graph.Equals(VersionGraphUri)));
            }
        }

    }
}
