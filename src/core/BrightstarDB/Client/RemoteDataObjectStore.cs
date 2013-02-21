using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Model;
using BrightstarDB.Rdf;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Use the WCF Client to talk to the service
    /// </summary>
    internal abstract class RemoteDataObjectStore : DataObjectStoreBase
    {
        private const string DataObjectQueryTemplate = @"select ?p ?o where {{ <{0}> ?p ?o }}";
        private readonly string _storeName;
        private readonly bool _optimisticLockingEnabled;

        protected RemoteDataObjectStore(string storeName, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled)
            : base(namespaceMappings)
        {
            _storeName = storeName;
            _optimisticLockingEnabled = optimisticLockingEnabled;
            ResetTransactionData();
        }

        #region Implementation of IDataObjectStore

        /// <summary>
        /// This must be overidden by all subclasses to create the correct client
        /// </summary>
        protected abstract IBrightstarService Client { get; }

        public override IDataObject GetDataObject(string identity)
        {
            if (identity == null) throw new ArgumentNullException("identity");
            var resolvedIdentity = ResolveIdentity(identity);
            var dataObject = new DataObject(this, resolvedIdentity);
            BindDataObject(dataObject);
            RegisterDataObject(dataObject);
            return dataObject;
        }

        /// <summary>
        /// Given an arbitrary query with exactly 1 result column, that result column will be used as the identity of
        /// a data object.
        /// </summary>
        /// <param name="sparqlExpression">Sparql Query</param>
        /// <returns>An enumeration of data objects</returns>
        public override IEnumerable<IDataObject> BindDataObjectsWithSparql(string sparqlExpression)
        {
            var binder = new SparqlResultDataObjectHelper(this);
            return binder.BindDataObjects(ExecuteSparql(sparqlExpression));
        }

        public override SparqlResult ExecuteSparql(string sparqlExpression)
        {
            return new SparqlResult(Client.ExecuteQuery(_storeName, sparqlExpression));
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
                        // no existing version information so assume this is the first
                        entity.SetProperty(Constants.VersionPredicateUri, 1);
                    }
                    else
                    {
                        var intVersion = Convert.ToInt32(version);
                        // inc version
                        intVersion++;
                        entity.SetProperty(Constants.VersionPredicateUri, intVersion);
                        Preconditions.Add(new Triple { Graph = Constants.DefaultGraphUri, DataType = RdfDatatypes.Integer, IsLiteral = true, LangCode = null, Object = version.ToString(), Predicate = Constants.VersionPredicateUri, Subject = subject });
                    }
                }
            }

            var deleteData = new StringWriter();
            var dw = new BrightstarTripleSinkAdapter(new NTriplesWriter(deleteData));
            foreach (Triple triple in DeletePatterns)
            {
                dw.Triple(triple);
            }
            deleteData.Close();

            var addData = new StringWriter();
            var aw = new BrightstarTripleSinkAdapter(new NTriplesWriter(addData));
            foreach (Triple triple in AddTriples)
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

            PostTransaction(preconditionsData.ToString(), deleteData.ToString(), addData.ToString());

            // reset changes
            ResetTransactionData();
        }


        private void PostTransaction(string preconditions, string patternsToDelete, string triplesToAdd)
        {
            var jobInfo = Client.ExecuteTransaction(_storeName, preconditions, patternsToDelete, triplesToAdd);
            while (!(jobInfo.JobCompletedOk || jobInfo.JobCompletedWithErrors))
            {
                Thread.Sleep(20);
                jobInfo = Client.GetJobInfo(_storeName, jobInfo.JobId);
            }

            if (jobInfo.JobCompletedWithErrors)
            {
                // if (jobInfo.ExceptionInfo.Type == typeof(Server.PreconditionFailedException).FullName)
                if ( jobInfo.ExceptionInfo != null && jobInfo.ExceptionInfo.Type == "BrightstarDB.Server.PreconditionFailedException")
                {
                    var triples = jobInfo.ExceptionInfo.Message.Substring(jobInfo.ExceptionInfo.Message.IndexOf('\n') + 1);
                    Preconditions.Clear();
                    throw new TransactionPreconditionsFailedException(triples);
                }
                throw new BrightstarClientException("Error processing update transaction. " + jobInfo.StatusMessage);
            }
        }

        #endregion

        public override bool BindDataObject(DataObject dataObject)
        {
            return dataObject.BindTriples(GetTriplesForDataObject(dataObject.Identity));
        }

        private IEnumerable<Triple> GetTriplesForDataObject(string identity)
        {
            Stream sparqlResultStream = Client.ExecuteQuery(_storeName, string.Format(DataObjectQueryTemplate, identity));
            XDocument data = XDocument.Load(sparqlResultStream);

            foreach (var sparqlResultRow in data.SparqlResultRows())
            {
                // create new triple
                var triple = new Triple
                {
                    Subject = identity,
                    Graph = Constants.DefaultGraphUri,
                    Predicate = sparqlResultRow.GetColumnValue("p").ToString()
                };

                if (sparqlResultRow.IsLiteral("o"))
                {
                    var dt = sparqlResultRow.GetLiteralDatatype("o");
                    var langCode = sparqlResultRow.GetLiteralLanguageCode("o");
                    triple.DataType = dt ?? RdfDatatypes.String;
                    if (langCode != null) {triple.LangCode = langCode;}
                    triple.Object = sparqlResultRow.GetColumnValue("o").ToString().Trim();
                    triple.IsLiteral = true;
                } else
                {
                    triple.Object = sparqlResultRow.GetColumnValue("o").ToString().Trim();                    
                }

                yield return triple;
            }
        }
    }
}