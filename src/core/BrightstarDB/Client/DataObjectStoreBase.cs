using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework.Query;
using BrightstarDB.Model;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Client
{
    /// <summary>
    /// Base class for data object store implementations
    /// </summary>
    internal abstract class DataObjectStoreBase : IInternalDataObjectStore
    {
        /// <summary>
        /// Mapping of CURIE prefix to namespace URI
        /// </summary>
        private readonly Dictionary<string, string> _namespaceMappings;

        /// <summary>
        /// The collection of dataobjects being managed by this store context
        /// </summary>
        private readonly Dictionary<string, DataObject> _managedProxies;

        /// <summary>
        /// Collection of triple deletions to send to the server on save
        /// </summary>
        private TripleCollection _deletePatterns;

        /// <summary>
        /// Collection of triple insertions to send to server on save
        /// </summary>
        private TripleCollection _addTriples;

        /// <summary>
        /// Collection of triples that must be present before the transaction can be executed.
        /// </summary>
        private TripleCollection _preconditions;

        /// <summary>
        /// Collection of triples that must no be present before the transaction can be executed.
        /// </summary>
        private TripleCollection _nonExistencePreconditions;

        private readonly bool _isReadOnly;

        private EventHandler<DataObjectStoreChangeEventArgs> _savingChanges;

        private readonly string _updateGraphUri;
        private readonly string[] _datasetGraphUris;
        private readonly string _datasetClause;
        private readonly string _versionGraphUri;

        private const string InverseOfSparql = "SELECT ?s WHERE {{ ?s <{0}> <{1}> }}";
        private const string GetVersionSparql = "SELECT ?v WHERE {{ <{0}> <" + Constants.VersionPredicateUri + "> ?v }}";
        private const string AllInverseOfSparql = "SELECT ?s ?p ?g WHERE {{ GRAPH ?g {{ ?s ?p <{0}> }} }}";

        private bool _disposed;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="asReadOnly">Set the new data object store to be readonly.</param>
        /// <param name="namespaceMappings">The initial set of CURIE prefix mappings</param>
        /// <param name="updateGraphUri">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="datasetGraphUris">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve data objects and their properties.
        /// If not defined, all graphs in the store will be queried.</param>
        /// <param name="versionGraphUri">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraphUri"/> will be used.</param>
        protected DataObjectStoreBase(bool asReadOnly, Dictionary<string, string> namespaceMappings,
            string updateGraphUri = null, IEnumerable<string> datasetGraphUris = null, string versionGraphUri = null)
        {
            _isReadOnly = asReadOnly;
            _namespaceMappings = namespaceMappings ?? new Dictionary<string, string>();
            _managedProxies = new Dictionary<string, DataObject>();

            //_updateGraphUri = String.IsNullOrEmpty(updateGraphUri) ? Constants.DefaultGraphUri : updateGraphUri;
            _updateGraphUri = updateGraphUri;

            _datasetGraphUris = datasetGraphUris == null ? null : datasetGraphUris.ToArray();
            if (_datasetGraphUris != null && _datasetGraphUris.Length == 0)
            {
                // caller provided an empty enumeration, so default to all graphs
                _datasetGraphUris = null;
                _datasetClause = String.Empty;
            }
            if (_datasetGraphUris != null)
            {
                var builder = new StringBuilder();
                builder.Append("FROM ");
                foreach (var g in _datasetGraphUris)
                {
                    builder.AppendFormat("<{0}> ", g);
                }
                _datasetClause = builder.ToString();
            }
            _versionGraphUri = String.IsNullOrEmpty(versionGraphUri) ? _updateGraphUri : versionGraphUri;

        }

        protected DataObject LookupDataObject(string identifier)
        {
            DataObject obj;
            _managedProxies.TryGetValue(identifier, out obj);
            return obj;
        }

        /// <summary>
        /// Checks whether the supplied identity is a valid Curie and if so resolves it against the supplied namespace mappings
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        protected string ResolveIdentity(string identity)
        {
            var curie = new Curie(identity);
            if (curie.IsValidCurie && _namespaceMappings.ContainsKey(curie.Prefix))
            {
                return Curie.ResolveCurie(curie, _namespaceMappings).ToString();
            }
            if (Uri.IsWellFormedUriString(identity, UriKind.Absolute))
            {
                return identity;
            }
            throw new ArgumentException(Strings.InvalidDataObjectIdentity, "identity");
        }

        /// <summary>
        /// Returns the tracked data object for a given input data object
        /// </summary>
        /// <param name="p">The data object to be tracked</param>
        /// <returns>The tracked data object for the same identity as <paramref name="p"/></returns>
        /// <remarks>If <paramref name="p"/> has an identity which matches an existing tracked data object,
        /// then the existing tracked data object is returned, otherwise <paramref name="p"/> is added
        /// to the data object tracker and returned.</remarks>
        protected DataObject RegisterDataObject(DataObject p)
        {
            DataObject existing;
            if (_managedProxies.TryGetValue(p.Identity, out existing)) return existing;
            _managedProxies.Add(p.Identity, p);
            return p;
        }

        /// <summary>
        /// Removes a data object from the tracker
        /// </summary>
        /// <param name="identity">The identity of the data object to be removed from the tracker</param>
        protected void DeregisterDataObject(string identity)
        {
            if (_managedProxies.ContainsKey(identity))
            {
                _managedProxies.Remove(identity);
            }
        }

        #region Implementation of IDataObjectStore

        public bool IsReadOnly { get { return _isReadOnly; } }

        public EventHandler<DataObjectStoreChangeEventArgs> SavingChanges
        {
            get { return _savingChanges; }
            set { _savingChanges = value; }
        }

        public IDataObject MakeNewDataObject(string prefix = null)
        {
            var identity = Constants.GeneratedUriPrefix + Guid.NewGuid();
            if (prefix != null)
            {
                identity = prefix + Guid.NewGuid();
            }
            return RegisterDataObject(new DataObject(this, identity, true));
        }

        /// <summary>
        /// Creates a new DataObject whose identity is a unique URI created by the client.
        /// </summary>
        /// <returns>A new DataObject</returns>
        public IDataObject MakeDataObject()
        {
            return RegisterDataObject(new DataObject(this));
        }

        public IDataObject MakeListDataObject(IEnumerable<object> listItems)
        {
            if (listItems == null) throw new ArgumentNullException("listItems");
            if (listItems.Count() == 0) throw new ArgumentException("List must contain at least 1 item");

            return BuildList(listItems);
        }

        private IDataObject BuildList(IEnumerable<object> values)
        {
            // end of list return rdf:nil 
            if (values.Count() == 0)
                return MakeDataObject("rdf:nil");

            return MakeDataObject()
                .SetType(MakeDataObject("rdf:List")).SetProperty("rdf:first", values.First())
                .SetProperty("rdf:rest", BuildList(values.Skip(1)));
        }

        /// <summary>
        /// The string value can either be a valid absolute URI or a CURIE.
        /// </summary>
        /// <param name="identity">The URI or CURIE that identifies the DataObject</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="identity"/> could not be parsed as a CURIE or a URI</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="identity"/> is NULL</exception>
        /// <returns></returns>
        public IDataObject MakeDataObject(string identity)
        {
            if (identity == null) throw new ArgumentNullException("identity");
            var curie = new Curie(identity);
            if (curie.IsValidCurie && _namespaceMappings.ContainsKey(curie.Prefix))
            {
                return RegisterDataObject(new DataObject(this, Curie.ResolveCurie(new Curie(identity), _namespaceMappings).ToString()));
            }
            
            Uri uri;
            var validUri = Uri.TryCreate(identity, UriKind.Absolute, out uri);
            if(validUri)
            {
                // Use uri.ToString() to allow for correct unescaping of identity
                return RegisterDataObject(new DataObject(this, uri.ToString()));
            }
            
            throw new ArgumentException(Strings.InvalidDataObjectIdentity, "identity");
        }

        public abstract IDataObject GetDataObject(string identity);
        public abstract IEnumerable<IDataObject> BindDataObjectsWithSparql(string sparqlExpression);

        public SparqlResult ExecuteSparql(string sparqlQuery)
        {
            return ExecuteSparql(new SparqlQueryContext(sparqlQuery));
        }
        public abstract SparqlResult ExecuteSparql(SparqlQueryContext sparqlQueryContext);

        /// <summary>
        /// Commits all changes. Waits for the operation to complete.
        /// </summary>
        public void SaveChanges()
        {
            if (_isReadOnly) throw new BrightstarStoreIsReadOnlyException();
            // Convert the triple collections to flat lists for processing the SavingChanges event and DoSaveChanges
            // This is inefficient but we are doing it for now to preserve API compatibilty between 1.7 and 1.8
            // For the 2.0 release this should be changed so that the event notification uses the the TripleCollection directly

            var addTriples = _addTriples.Items.ToList();
            var deletePatterns = _deletePatterns.Items.ToList();
            var preconditions = _preconditions.Items.ToList();
            var nePreconditions = _nonExistencePreconditions.Items.ToList();
            if (_savingChanges != null)
            {
                _savingChanges(this, new DataObjectStoreChangeEventArgs(
                    addTriples,
                    deletePatterns,
                    preconditions,
                    nePreconditions));
            }
            DoSaveChanges();
        }

        protected abstract void DoSaveChanges();

        /// <summary>
        /// Removes the specified data object from the tracking of the store so that it will not be part of the next SaveChanges
        /// </summary>
        /// <param name="dataObject"></param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dataObject"/> is NULL</exception>
        public void DetachDataObject(IDataObject dataObject)
        {
            if (dataObject == null) throw new ArgumentNullException("dataObject");
            DeregisterDataObject(dataObject.Identity);
        }

        public void Refresh(RefreshMode mode, IDataObject dataObject)
        {
            if (dataObject == null) throw new ArgumentNullException("dataObject");
            if (_managedProxies[dataObject.Identity] != dataObject)
            {
                throw new ArgumentException("Data object is not tracked by this store.", "dataObject");
            }

            if (mode == RefreshMode.ClientWins)
            {
                // We just lookup the new version number
                UpdateVersionFromSparqlResult(
                    ExecuteSparql(new SparqlQueryContext(String.Format(GetVersionSparql, dataObject.Identity))),
                    dataObject);
            }
            else
            {
                var managed = _managedProxies[dataObject.Identity];
                BindDataObject(managed);
                // Reset all updates for the bound object
                _addTriples.RemoveBySubject(managed.Identity);
                _deletePatterns.RemoveBySubject(managed.Identity);
            }
        }

        public IEnumerable<IDataObject> TrackedObjects
        {
#if WINDOWS_PHONE || PORTABLE
            get { return _managedProxies.Values.Cast<IDataObject>(); }
#else
            get { return _managedProxies.Values; }
#endif
        }

        public IList<string> GetDataset()
        {
            return _datasetGraphUris;
        }

        public string GetDatasetClause()
        {
            return _datasetClause;
        }

        public void AddPrecondition(bool matchExisting, string subject, string predicate, string @object, string graph = Constants.WildcardUri,
                                    bool isLiteral = false, string datatype = null, string language = null)
        {
            var t = new Triple
                {
                    Subject = subject,
                    Predicate = predicate,
                    Object = @object,
                    Graph = graph,
                    IsLiteral = isLiteral,
                    DataType = datatype,
                    LangCode = language
                };
            if (matchExisting)
            {
                Preconditions.Add(t);
            }
            else
            {
                NonExistencePreconditions.Add(t);
            }
        }

        #endregion

        #region Implementation of IInternalDataObjectStore

        public abstract bool BindDataObject(DataObject dataObject);

        /// <summary>
        /// The URI identifier of the graph to be updated
        /// </summary>
        public string UpdateGraphUri { get { return _updateGraphUri; } }

        /// <summary>
        /// The URI identifiers of the graphs that contribute properties
        /// </summary>
        public string[] DataSetGraphUris { get { return _datasetGraphUris; } }

        /// <summary>
        /// The URI identifier of the graph that stores data object version numbers
        /// </summary>
        public string VersionGraphUri { get { return _versionGraphUri; } }

        /// <summary>
        /// The current transaction delete patterns
        /// </summary>
        public ITripleCollection DeletePatterns { get { return _deletePatterns; } }

        /// <summary>
        /// The current transaction triples to add
        /// </summary>
        public ITripleCollection AddTriples { get { return _addTriples; } }

        /// <summary>
        /// The current transaction preconditions
        /// </summary>
        public ITripleCollection Preconditions { get { return _preconditions; } }

        public ITripleCollection NonExistencePreconditions { get { return _nonExistencePreconditions; } }
        /// <summary>
        /// Returns an enumeration of all data objects that are the subject
        /// of a triple that binds a predicate of type <paramref name="pred"/>
        /// and the object <paramref name="obj"/>, optionally filtered by
        /// the subject resource type.
        /// </summary>
        /// <param name="obj">The object resource</param>
        /// <param name="pred">The predicate resource</param>
        /// <returns>An enumeration of all matching subject resources</returns>
        public IEnumerable<IDataObject> GetInverseOf(IDataObject obj, IDataObject pred)
        {
            var queryResults = BindDataObjectsWithSparql(String.Format(InverseOfSparql, pred.Identity, obj.Identity));
            var matchTriple = new Triple
                                   {
                                       Subject = null,
                                       Predicate = pred.Identity,
                                       Object = obj.Identity,
                                       IsLiteral = false
                                   };
            foreach(var x in queryResults)
            {
                matchTriple.Subject = x.Identity;
                if(!_deletePatterns.GetMatches(matchTriple).Any())
                {
                    yield return x;
                }
            }
            matchTriple.Subject = null;
            foreach(var addTriple in _addTriples.GetMatches(matchTriple))
            {
                yield return MakeDataObject(addTriple.Subject);
            }
        }


        public IEnumerable<Triple> GetReferencingTriples(IDataObject obj)
        {
            return GetReferencingTriples(obj.Identity);
        }

        public IEnumerable<Triple> GetReferencingTriples(string identity)
        {
            var queryResults = ExecuteSparql(String.Format(AllInverseOfSparql, identity));
            return queryResults.ResultDocument.SparqlResultRows().Select(row => new Triple
            {
                Subject = row.GetColumnValue("s").ToString(),
                Predicate = row.GetColumnValue("p").ToString(),
                Object = identity,
                IsLiteral = false,
                Graph = row.GetColumnValue("g").ToString()
            });
        } 

        /// <summary>
        /// Adds preconditions to validate that there is no existing resource with the URI
        /// <paramref name="identity"/> that is an instance of one or more of the specified types.
        /// </summary>
        /// <param name="identity">The identity of the resource to be validated</param>
        /// <param name="types">An enumeration of class resources URIs</param>
        public void SetClassUniqueConstraints(string identity, IEnumerable<string> types)
        {
            _nonExistencePreconditions.RemoveBySubjectPredicate(identity, DataObject.TypeDataObject.Identity);
            _nonExistencePreconditions.AddRange(
                types.Select(x=>new Triple{Subject = identity, Predicate = DataObject.TypeDataObject.Identity, Object = x, Graph = Constants.WildcardUri}));
        }

        public void ReplaceIdentity(string oldIdentity, string newIdentity)
        {
            // Replace any references in the AddTriples collection
            foreach (var addTriple in AddTriples.GetMatches(new Triple{Subject=null, Predicate=null, Object=oldIdentity, IsLiteral = false}).ToList())
            {
                AddTriples.Add(new Triple
                {
                    Subject = addTriple.Subject,
                    Predicate = addTriple.Predicate,
                    Object = newIdentity,
                    Graph = addTriple.Graph
                });
            }
            AddTriples.RemoveByObject(oldIdentity);

            DataObject dataObject;
            var isNew = _managedProxies.TryGetValue(oldIdentity, out dataObject) && dataObject.IsNew;
            if (!isNew)
            {
                // There may be some references in the store that are not currently materialised on the client
                // So query for those and ensure an update
                foreach (var refTriple in GetReferencingTriples(oldIdentity))
                {
                    AddTriples.Add(new Triple
                    {
                        Subject = refTriple.Subject,
                        Predicate = refTriple.Predicate,
                        Object = newIdentity,
                        Graph = refTriple.Graph
                    });
                }
                DeletePatterns.Add(new Triple
                {
                    Subject = Constants.WildcardUri,
                    Predicate = Constants.WildcardUri,
                    Object = oldIdentity,
                    Graph = _updateGraphUri
                });
            }

            // Remove class unique constraints for the old identiy (if any)
            _nonExistencePreconditions.RemoveBySubjectPredicate(oldIdentity, DataObject.TypeDataObject.Identity);
        }

        #endregion

        protected void ResetTransactionData()
        {
            foreach (var managedProxy in _managedProxies.Values)
            {
                managedProxy.IsNew = false;
            }
            _deletePatterns = new TripleCollection();
            _addTriples = new TripleCollection();
            _preconditions = new TripleCollection();
            _nonExistencePreconditions = new TripleCollection();
        }

        private static void UpdateVersionFromSparqlResult(SparqlResult sparqlResult, IDataObject dataObject)
        {
            var firstRow = sparqlResult.ResultDocument.SparqlResultRows().FirstOrDefault();
            var value = firstRow.GetColumnValue("v");
            dataObject.SetProperty(Constants.VersionPredicateUri, value);
        }

        ~DataObjectStoreBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Cleanup();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// This method is invoked when the store is being disposed.
        /// </summary>
        protected abstract void Cleanup();
    }
}
