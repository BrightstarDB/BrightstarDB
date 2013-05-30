#if !REST_CLIENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        internal EmbeddedDataObjectStore(ServerCore serverCore, string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled)
            : base(namespaceMappings)
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
                IEnumerable<Triple> triples =
                    _serverCore.GetResourceStatements(_storeName, resolvedIdentity).Union(
                        AddTriples.Where(p => p.Subject.Equals(resolvedIdentity)));
                registeredDataObject.BindTriples(triples);
            }
            return registeredDataObject;
        }

        public override IEnumerable<IDataObject> BindDataObjectsWithSparql(string sparqlExpression)
        {
            var helper = new SparqlResultDataObjectHelper(this);
            return helper.BindDataObjects(ExecuteSparql(sparqlExpression));
        }

        public override SparqlResult ExecuteSparql(string sparqlExpression)
        {
            var xml = _serverCore.Query(_storeName, sparqlExpression, SparqlResultsFormat.Xml);
            return new SparqlResult(xml);
        }

        public override bool BindDataObject(DataObject dataObject)
        {
            IEnumerable<Triple> triples = _serverCore.GetResourceStatements(_storeName, dataObject.Identity);
            return dataObject.BindTriples(triples);
        }

        /// <summary>
        /// Commits all changes. Waits for the operation to complete.
        /// </summary>
        protected override void DoSaveChanges()
        {
            if (_optimisticLockingEnabled)
            {
                // get subject entity and see if there is a version triple
                var subjects = AddTriples.Select(x => x.Subject).Distinct().Union(DeletePatterns.Select(x => x.Subject).Distinct()).ToList();
                foreach (var subject in subjects)
                {
                    var entity = LookupDataObject(subject);
                    if (entity == null) throw new BrightstarClientException("No Entity Found for Subject " + subject);

                    var version = entity.GetPropertyValue(Constants.VersionPredicateUri);
                    if (version == null)
                    {
                        // no existing version information so assume this is the first time using it with OL
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
                                                    Graph = Constants.DefaultGraphUri,
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
            var dw = new BrightstarTripleSinkAdapter(new NTriplesWriter(deleteData));
            foreach (var triple in DeletePatterns)
            {
                dw.Triple(triple);
            }
            deleteData.Close();

            var addData = new StringWriter();
            var aw = new BrightstarTripleSinkAdapter(new NTriplesWriter(addData));
            foreach (var triple in AddTriples)
            {
                aw.Triple(triple);               
            }
            addData.Close();

            var preconditionsData = new StringWriter();
            var pw = new BrightstarTripleSinkAdapter(new NTriplesWriter(preconditionsData));
            foreach (var triple in Preconditions)
            {
                pw.Triple(triple);
            }
            preconditionsData.Close();

            var jobId = _serverCore.ProcessTransaction(_storeName, preconditionsData.ToString(), deleteData.ToString(), addData.ToString(), Constants.DefaultGraphUri);
            var status = _serverCore.GetJobStatus(_storeName, jobId.ToString());
            while (!(status.JobStatus == JobStatus.CompletedOk || status.JobStatus == JobStatus.TransactionError))
            {
                // wait for completion.
                Thread.Sleep(5);
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
                // todo: fix me and report inner exception
                throw new BrightstarClientException(status.ExceptionDetail != null ? status.ExceptionDetail.Message : "The transaction encountered an error");
            }

            // reset changes
            ResetTransactionData();
        }

        #endregion
    }
}
#endif