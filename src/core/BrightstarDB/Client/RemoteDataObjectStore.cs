using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.EntityFramework.Query;
using BrightstarDB.Model;
using BrightstarDB.Rdf;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Base class for store implementations that talk to a remote service
    /// </summary>
    internal abstract class RemoteDataObjectStore : DataObjectStoreBase
    {
        private readonly bool _optimisticLockingEnabled;
        private string _queryTemplate;

        private String QueryTemplate { get { return _queryTemplate ?? (_queryTemplate = GetQueryTemplate()); } }

        protected RemoteDataObjectStore(bool asReadOnly, Dictionary<string, string> namespaceMappings, bool optimisticLockingEnabled,
            string updateGraphUri = null, IEnumerable<string> datasetGraphUris = null, string versionGraphUri = null)
            : base(asReadOnly, namespaceMappings, updateGraphUri, datasetGraphUris, versionGraphUri)
        {
            _optimisticLockingEnabled = optimisticLockingEnabled;
            ResetTransactionData();
        }

        #region Implementation of IDataObjectStore

        /// <summary>
        /// This must be overidden by all subclasses to create the correct client
        /// </summary>
        protected abstract IUpdateableStore Client { get; }

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
            return binder.BindDataObjects(ExecuteSparql(new SparqlQueryContext(sparqlExpression)));
        }

        public override SparqlResult ExecuteSparql(SparqlQueryContext sparqlQueryContext)
        {
            return new SparqlResult(Client.ExecuteQuery(sparqlQueryContext.SparqlQuery, DataSetGraphUris), sparqlQueryContext);
        }

        protected virtual string GetQueryTemplate()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ?p ?o ?g");
            if (DataSetGraphUris != null)
            {
                foreach (var dsGraph in DataSetGraphUris)
                {
                    sb.AppendFormat(" FROM NAMED <{0}>", dsGraph);
                }
            }
            if (UpdateGraphUri != null)
            {
                sb.AppendFormat(" FROM NAMED <{0}>", UpdateGraphUri);
            }
            if (VersionGraphUri != null)
            {
                sb.AppendFormat(" FROM NAMED <{0}>", VersionGraphUri);
            }
            sb.Append(" WHERE {{ GRAPH ?g {{ <{0}> ?p ?o }} }}");
            return sb.ToString();
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
                              .Except(new[] {Constants.WildcardUri})
                              .ToList();
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
                        Preconditions.Add(new Triple
                            {
                                Subject = subject,
                                Predicate = Constants.VersionPredicateUri,
                                Object = version.ToString(),
                                IsLiteral = true,
                                DataType = RdfDatatypes.Integer,
                                LangCode = null, 
                                Graph = VersionGraphUri
                            });
                    }
                }
            }

            try
            {
                Client.ApplyTransaction(Preconditions, DeletePatterns, AddTriples, UpdateGraphUri);
            }
            catch (TransactionPreconditionsFailedException)
            {
                Preconditions.Clear();
                throw;
            }

            // reset changes
            ResetTransactionData();
        }

        #endregion

        public override bool BindDataObject(DataObject dataObject)
        {
            return dataObject.BindTriples(GetTriplesForDataObject(dataObject.Identity));
        }

        private IEnumerable<Triple> GetTriplesForDataObject(string identity)
        {
            Stream sparqlResultStream = Client.ExecuteQuery(string.Format(QueryTemplate, identity), DataSetGraphUris);
            XDocument data = XDocument.Load(sparqlResultStream);

            foreach (var sparqlResultRow in data.SparqlResultRows())
            {
                // create new triple
                var triple = new Triple
                {
                    Subject = identity,
                    Graph = sparqlResultRow.GetColumnValue("g").ToString(),
                    Predicate = sparqlResultRow.GetColumnValue("p").ToString()
                };

                if (sparqlResultRow.IsLiteral("o"))
                {
                    var dt = sparqlResultRow.GetLiteralDatatype("o");
                    var langCode = sparqlResultRow.GetLiteralLanguageCode("o");
                    triple.DataType = dt ?? RdfDatatypes.String;
                    if (langCode != null)
                    {
                        triple.LangCode = langCode;
                    }
                    triple.Object = sparqlResultRow.GetColumnValue("o").ToString().Trim();
                    triple.IsLiteral = true;
                }
                else
                {
                    triple.Object = sparqlResultRow.GetColumnValue("o").ToString().Trim();
                }

                yield return triple;
            }
        }

        protected override void Cleanup()
        {
            Client.Cleanup();
        }
    }
}