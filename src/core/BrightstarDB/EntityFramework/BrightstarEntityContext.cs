using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework.Query;
using BrightstarDB.Rdf;
using BrightstarDB.Storage.Persistence;
using Remotion.Linq.Clauses;

#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The base class for Brightstar EntityFramework domain context classes backed by a Brightstar IDataStore client.
    /// </summary>
    public class BrightstarEntityContext : EntityContext
    {
        private readonly IDataObjectStore _store;
        private readonly Dictionary<string, List<BrightstarEntityObject>> _trackedObjects;

        /// <summary>
        /// Creates a new domain context
        /// </summary>
        /// <param name="store">The Brightstar store that manages the data</param>
        protected BrightstarEntityContext(IDataObjectStore store)
        {
            _store = store;
            _trackedObjects = new Dictionary<string, List<BrightstarEntityObject>>();
        }

        /// <summary>
        /// Creates a new domain context
        /// </summary>
        ///<param name="connectionString">The connection string that will be used to connect to an existing BrightstarDB store</param>
        ///<param name="enableOptimisticLocking">Optional parameter to override the optimistic locking configuration specified in the connection string</param>
        /// <param name="updateGraphUri">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="datasetGraphUris">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve entities and their properties. See
        /// the remarks below.</param>
        /// <param name="versionGraphUri">OPTIONAL: The URI identifier of the graph that contains version number statements for entities. 
        /// If not defined, the <paramref name="updateGraphUri"/> will be used.</param>
        /// <remarks>
        /// <para>If <paramref name="datasetGraphUris"/> is null, then the context will query the graphs defined by 
        /// <paramref name="updateGraphUri"/> and <paramref name="versionGraphUri"/> only. If all three parameters
        /// are null then the context will query across all graphs in the store.</para>
        /// </remarks>
        protected BrightstarEntityContext(string connectionString, bool? enableOptimisticLocking = null,
            string updateGraphUri = null, IEnumerable<string> datasetGraphUris = null, string versionGraphUri = null )
        {
            var cstr = new ConnectionString(connectionString);
            AssertStoreFromConnectionString(cstr);
            _store = OpenStore(cstr, enableOptimisticLocking,
                updateGraphUri, datasetGraphUris, versionGraphUri);
            _trackedObjects = new Dictionary<string, List<BrightstarEntityObject>>();
        }

        /// <summary>
        /// Occurs when changes are saved to the domain context
        /// </summary>
        /// <remarks>The <see cref="SavingChanges"/> event is raised at the start of a <see cref="SaveChanges"/> operation on a <see cref="BrightstarEntityContext"/>.</remarks>
        public EventHandler SavingChanges;

        /// <summary>
        /// Provides an enumeration over all entities currently tracked by this domain context
        /// </summary>
        public IEnumerable<BrightstarEntityObject> TrackedObjects { get { return _trackedObjects.Values.SelectMany(v=>v); } }

        private static void AssertStoreFromConnectionString(ConnectionString connectionString)
        {
            if (connectionString.Type == ConnectionType.DotNetRdf || connectionString.Type == ConnectionType.Sparql)
            {
                return;
            }
#if SILVERLIGHT            
            var service = new EmbeddedBrightstarService(connectionString.StoresDirectory);
#else
            var service = BrightstarService.GetClient(connectionString.Value);
#endif
            if (!service.DoesStoreExist(connectionString.StoreName))
            {
                service.CreateStore(connectionString.StoreName);
            }
        }

        /// <summary>
        /// Creates a new domain context and connects to the store specified in the configuration connectionString.
        /// </summary>
        /// <param name="updateGraphUri">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="datasetGraphUris">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve entities and their properties.
        /// If not defined, all graphs in the store will be queried.</param>
        /// <param name="versionGraphUri">OPTIONAL: The URI identifier of the graph that contains version number statements for entities. 
        /// If not defined, the <paramref name="updateGraphUri"/> will be used.</param>
        protected BrightstarEntityContext(
            string updateGraphUri = null, IEnumerable<string> datasetGraphUris = null, string versionGraphUri = null)
        {
            var cstr = new ConnectionString(Configuration.ConnectionString);
            AssertStoreFromConnectionString(cstr);
            _store = OpenStore(cstr, updateGraphUri:updateGraphUri, datasetGraphUris:datasetGraphUris, versionGraphUri:versionGraphUri);
            _trackedObjects = new Dictionary<string, List<BrightstarEntityObject>>();
        }

        private static IDataObjectStore OpenStore(ConnectionString connectionString, bool? enableOptimisticLocking = null,
            string updateGraphUri = null, IEnumerable<string> datasetGraphUris = null, string versionGraphUri = null)
        {
            IDataObjectContext context;
            switch (connectionString.Type)
            {
                case ConnectionType.Embedded:
                    context = new EmbeddedDataObjectContext(connectionString);
                    break;
#if !SILVERLIGHT && !__MonoCS__
                case ConnectionType.Rest:
                    context = new RestDataObjectContext(connectionString);
                    break;
#endif
                case ConnectionType.DotNetRdf:
                    context = BrightstarService.GetDataObjectContext(connectionString);
                    //context = new DotNetRdfDataObjectContext(connectionString);
                    break;

                case ConnectionType.Sparql:
                    context = BrightstarService.GetDataObjectContext(connectionString);
                    break;

                default:
                    throw new BrightstarClientException("Unable to create valid context with connection string " +
                                                        connectionString.Value);
            }
            return context.OpenStore(connectionString.StoreName,
                                     optimisticLockingEnabled:
                                         enableOptimisticLocking.HasValue
                                             ? enableOptimisticLocking.Value
                                             : connectionString.OptimisticLocking,
                                     updateGraph: updateGraphUri,
                                     defaultDataSet: datasetGraphUris,
                                     versionTrackingGraph: versionGraphUri);
        }

        internal IDataObject GetDataObject(Uri identity, bool loadNow)
        {
            if (loadNow)
            {
                var dataObject = _store.GetDataObject(identity.ToString());
                if (dataObject == null)
                {
                    throw new EntityFrameworkException(
                        String.Format("Could not find resource with identity {0}", identity));
                }
                return dataObject;
            }
            return _store.MakeDataObject(identity.ToString());
        }

        internal void TrackObject(BrightstarEntityObject obj)
        {
            List<BrightstarEntityObject> trackedObjects;
            if (obj.DataObject == null)
            {
                // Don't track the object until it has an underlying data object
                return;
            }
            if (_trackedObjects.TryGetValue(obj.DataObject.Identity, out trackedObjects))
            {
                if (!trackedObjects.Contains(obj)) trackedObjects.Add(obj);
            }
            else
            {
                trackedObjects = new List<BrightstarEntityObject> {obj};
                _trackedObjects[obj.DataObject.Identity] = trackedObjects;
            }
        }

        internal void UntrackObject(BrightstarEntityObject obj)
        {
            if (obj.DataObject == null)
            {
                // No data object to provide a key for lookup
                return;
            }
            if (_trackedObjects.ContainsKey(obj.DataObject.Identity))
            {
                _trackedObjects.Remove(obj.DataObject.Identity);
            }
            _store.DetachDataObject(obj.DataObject);
        }

        #region Overrides of EntityContext

        /// <summary>
        /// Commit local changes to the underlying store
        /// </summary>
        public override void SaveChanges()
        {
            if (SavingChanges != null)
            {
                SavingChanges(this, new EventArgs());
            }
            EnsureIdentity();
            try
            {
                _store.SaveChanges();
            }
            catch (TransactionPreconditionsFailedException ex)
            {
                if (ex.InvalidNonExistenceSubjects.Any())
                {
                    throw new UniqueConstraintViolationException(ex.InvalidNonExistenceSubjects);
                }
                throw;
            }
        }

        /// <summary>
        /// Ensure that each of the tracked objects in the context 
        /// that have key properties have those key properties set.
        /// </summary>
        private void EnsureIdentity()
        {
            foreach (var entry in _trackedObjects)
            {
                foreach (var item in entry.Value)
                {
                    if (item.IsModified)
                    {
                        EnsureIdentity(item);
                    }
                }
            }
        }

        private void EnsureIdentity(IEntityObject item)
        {
            IdentityInfo identityInfo = EntityMappingStore.GetIdentityInfo(item.GetType());
            if (identityInfo != null && identityInfo.KeyConverter != null)
            {
                var propertyValues = new object[identityInfo.KeyProperties.Length];
                for (var i = 0; i < identityInfo.KeyProperties.Length; i++)
                {
                    propertyValues[i] = identityInfo.KeyProperties[i].GetValue(item, null);
                }
                var key = identityInfo.KeyConverter.GenerateKey(propertyValues, identityInfo.KeySeparator, item.GetType());
                if (key == null) throw new EntityKeyRequiredException();
                if (!key.Equals(item.GetKey()))
                {
                    throw new EntityKeyChangedException();
                }
            }
        }

        /*
        internal IdentityInfo GetIdentityInfo(Type t)
        {
            IdentityInfo cachedInfo;
            if (_identityCache.TryGetValue(t, out cachedInfo)) return cachedInfo;

            var baseUri = Constants.GeneratedUriPrefix;
            PropertyInfo[] keyProperties = null;
            var keySeparator = DefaultCompositeKeySeparator;
            IKeyConverter keyConverter = null;

            var interfaces = t.GetInterfaces().Where(i => i.GetCustomAttributes(typeof(EntityAttribute), true).Any());
            var identityProperty =
                interfaces.SelectMany(i => i.GetProperties()).FirstOrDefault(
                    x => x.GetCustomAttributes(typeof(IdentifierAttribute), true).Any());
            if (identityProperty != null)
            {
                var identityAttr =
                    identityProperty.GetCustomAttributes(typeof(IdentifierAttribute), true).FirstOrDefault() as
                    IdentifierAttribute;
                var declaringType = identityProperty.DeclaringType;
                if (identityAttr != null)
                {
                    if (identityAttr.BaseAddress != null && identityAttr.BaseAddress.Contains(":"))
                    {
                        var prefix = identityAttr.BaseAddress.Substring(0, identityAttr.BaseAddress.IndexOf(':'));
                        var namespaceDecl = identityProperty.DeclaringType == null ? null :
                            identityProperty.DeclaringType.Assembly.GetCustomAttributes(
                                typeof(NamespaceDeclarationAttribute), false).Cast<NamespaceDeclarationAttribute>().
                                FirstOrDefault(nda => nda.Prefix.Equals(prefix));
                        if (namespaceDecl != null)
                        {
                            baseUri = namespaceDecl.Reference +
                                      identityAttr.BaseAddress.Substring(identityAttr.BaseAddress.IndexOf(':') + 1);
                        }
                        else
                        {
                            baseUri = identityAttr.BaseAddress;
                        }
                    }

                    if (identityAttr.KeyProperties != null && declaringType != null)
                    {
                        keyProperties = new PropertyInfo[identityAttr.KeyProperties.Length];
                        for (int i = 0; i < identityAttr.KeyProperties.Length; i++)
                        {
                            var propertyName = identityAttr.KeyProperties[i];
                            var propertyInfo = declaringType.GetProperty(propertyName);
                            if (propertyInfo == null)
                            {
                                throw new EntityFrameworkException(
                                    "Cannot find declared (composite) key property '{0}' on type '{1}'.", propertyName,
                                    declaringType.FullName);
                            }
                            keyProperties[i] = propertyInfo;
                        }
                        keySeparator = identityAttr.KeySeparator ?? DefaultCompositeKeySeparator;
                        if (identityAttr.KeyConverterType != null)
                        {
                            keyConverter = Activator.CreateInstance(identityAttr.KeyConverterType) as IKeyConverter;
                            if (keyConverter == null)
                            {
                                throw new EntityFrameworkException(
                                    "Cannot instantiate class {0} as an IKeyConverter instance.", identityAttr.KeyConverterType);
                            }
                        }
                        else
                        {
                            keyConverter = new DefaultKeyConverter();
                        }

                    }

                }
            }
            cachedInfo = new IdentityInfo(baseUri, keyProperties, keySeparator, keyConverter);
            _identityCache[t] = cachedInfo;
            return cachedInfo;
        }
         */
        /// <summary>
        /// Updates a single object in the object context with data from the data source
        /// </summary>
        /// <param name="mode">A <see cref="RefreshMode"/> value that indicates whether property changes
        /// in the object context are overwritten with property changes from the data source</param>
        /// <param name="entity">The object to be refreshed</param>
        public override void Refresh(RefreshMode mode, object entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            if (!(entity is BrightstarEntityObject)) throw new ArgumentException("Expected entity to extend the BrightstarEntityObject class", "entity");
            var beo = entity as BrightstarEntityObject;
            if (beo.Context != this) throw new ArgumentException("Entity is not attached to this context", "entity");

            if (mode == RefreshMode.ClientWins)
            {
                _store.Refresh(RefreshMode.ClientWins, beo.DataObject);
                beo.ForceCollectionPropertyUpdates();
            }
            else
            {
                _store.Refresh(RefreshMode.StoreWins, beo.DataObject);
                beo.ClearPropertyCache();
            }
        }

        /// <summary>
        /// Update a collection of objects in the object context with data from the data source
        /// </summary>
        /// <param name="mode">A <see cref="RefreshMode"/> value that indicates whether property changes
        /// in the object context are overwritten with property changes from the data source</param>
        /// <param name="entities">The objects to be refreshed</param>
        public override void Refresh(RefreshMode mode, IEnumerable entities)
        {
            foreach(var e in entities) Refresh(mode, e);
        }

        /// <summary>
        /// Method invoked to execute a SPARQL query against the underlying store
        /// </summary>
        /// <param name="sparqlQuery">The query to execute</param>
        /// <returns></returns>
        public override XDocument ExecuteQuery(string sparqlQuery)
        {
            return _store.ExecuteSparql(sparqlQuery).ResultDocument;
        }

        #region Bindings

        private T BindDataObject<T>(IDataObject dataObject, Type bindType)
        {
            List<BrightstarEntityObject> trackedObjects;
            if (_trackedObjects.TryGetValue(dataObject.Identity, out trackedObjects))
            {
                T matchObject = trackedObjects.OfType<T>().FirstOrDefault();
                if (matchObject == null)
                {
                    matchObject = (T)Activator.CreateInstance(bindType, this, dataObject);
                }
                return matchObject;
            }
            return ((T)Activator.CreateInstance(bindType, this, dataObject));
        }


        private Dictionary<Type, IList<BrightstarEntityObject>> BindSparqlResultToResultDefinedTypes(SparqlResult sparqlQueryResult)
        {
            var result = new Dictionary<Type, IList<BrightstarEntityObject>>();
            var helper = new SparqlResultDataObjectHelper(_store);
            IEnumerable<IDataObject> boundObjects = helper.BindDataObjects(sparqlQueryResult);

            //converts each object to types defined in it's triples
            foreach (var dataObject in boundObjects)
            {
                var types = dataObject.GetTypes();
                foreach (var typeUri in types)
                {
                    Type bindType = GetTypeForUri(typeUri);
                    var value = (BrightstarEntityObject)Convert.ChangeType(
                        BindDataObject<BrightstarEntityObject>(dataObject, bindType),
                        bindType, null);
                    if (!result.ContainsKey(bindType))
                    {
                        result.Add(bindType, new List<BrightstarEntityObject>());
                    }
                    result[bindType].Add(value);
                }
            }

            return result;
        }

        private IEnumerable<T> BindSparqlResultToKnownType<T>(SparqlResult sparqlQueryResult, IList<OrderingDirection> orderingDirections)
        {
            var helper = new SparqlResultDataObjectHelper(_store);
            IEnumerable<IDataObject> boundObjects = helper.BindDataObjects(sparqlQueryResult, orderingDirections);
            var bindType = GetImplType(typeof(T));
            foreach (var dataObject in boundObjects)
            {
                yield return BindDataObject<T>(dataObject, bindType);
            }
        }

        private IEnumerable<T> BindRowsToEntites<T>(SparqlResult sparqlQueryResult)
        {
            var sparqlResult = sparqlQueryResult;
            var resultDoc = sparqlResult.ResultDocument;
            var converter = GetStringConverter(typeof(T));
            if (converter == null)
            {
                throw new EntityFrameworkException("No SPARQL results conversion found from string to type '{0}'", typeof(T).FullName);
            }
            foreach (var row in resultDoc.SparqlResultRows())
            {
                var value = row.GetColumnValue(0);
                if (value == null) yield return default(T);
                if (value.GetType() == typeof(T))
                {
                    yield return (T)value;
                }
                else
                {
                    yield return (T)converter(row.GetColumnValue(0).ToString(), row.GetLiteralLanguageCode(0));
                }
            }
        }

        private IEnumerable<T> BindWithLinq<T>(SparqlLinqQueryContext sparqlLinqQueryContext)
        {
            if (sparqlLinqQueryContext.HasMemberInitExpression)
            {
                var sparqlResult = _store.ExecuteSparql(sparqlLinqQueryContext);
                var resultDoc = sparqlResult.ResultDocument;
                foreach (var row in resultDoc.SparqlResultRows())
                {
                    var values = new Dictionary<string, object>();
                    foreach (var c in resultDoc.GetVariableNames())
                    {
                        values[c] = row.GetColumnValue(c);
                    }
                    var value = sparqlLinqQueryContext.ApplyMemberInitExpression<T>(values, ConvertString);
                    yield return (T)value;
                }
            }
        }

        private IEnumerable<T> BindSparqlResultToAnonymousType<T>(SparqlResult sparqlQueryResult, List<Tuple<string, string>> anonymousMembersMap)
        {
            if (IsAnonymousType(typeof(T)))
            {
                var anonymousConstructorArgs = new List<AnonymousConstructorArg>();
                foreach (var tuple in anonymousMembersMap)
                {
                    var propertyInfo = typeof(T).GetProperty(tuple.Item1);
                    if (propertyInfo == null) throw new EntityFrameworkException("No property named '{0}' on anonymous type", tuple.Item1);
                    var propertyType = propertyInfo.PropertyType;
                    var converter = GetStringConverter(propertyType);
                    if (converter == null) throw new EntityFrameworkException("No converter available for type '{0}'", propertyType.FullName);
                    object defaultValue = propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
                    anonymousConstructorArgs.Add(new AnonymousConstructorArg
                    {
                        PropertyName = tuple.Item1,
                        VariableName = tuple.Item2,
                        ValueConverter = converter,
                        DefaultValue = defaultValue
                    });
                }

                var sparqlResult = sparqlQueryResult;
                var resultDoc = sparqlResult.ResultDocument;
                foreach (var row in resultDoc.SparqlResultRows())
                {
                    var args = new object[anonymousConstructorArgs.Count];
                    for (int i = 0; i < anonymousConstructorArgs.Count; i++)
                    {
                        var argInfo = anonymousConstructorArgs[i];
                        var colValue = row.GetColumnValue(argInfo.VariableName);
                        var colLang = row.GetLiteralLanguageCode(argInfo.VariableName);
                        args[i] = colValue == null ? argInfo.DefaultValue : argInfo.ValueConverter(colValue.ToString(), colLang);
                    }
                    yield return (T)Activator.CreateInstance(typeof(T), args);
                }
            }
        }

        #endregion

        /// <summary>
        /// Executes a SPARQL query against the underlying store and binds the results to
        /// domain context objects based on the info (rdf:type) found in the
        /// </summary>
        /// <param name="sparqlQuery">The query to be executed</param>
        /// <returns>A dictionary with a list of entities for each found type</returns>
        /// <example>Each type can be retrieved: 
        /// <![CDATA[ results[typeof(Person)].Cast<Person>().ToList()]]></example>
        public Dictionary<Type, IList<BrightstarEntityObject>> ExecuteQueryToResultTypes(string sparqlQuery)
        {
            var sparqlResult = _store.ExecuteSparql(sparqlQuery);

            return BindSparqlResultToResultDefinedTypes(sparqlResult);
        }

        /// <summary>
        /// Executes a SPARQL query against the underlying store and binds the results to
        /// domain context objects
        /// </summary>
        /// <typeparam name="T">The type of domain context object to bind to</typeparam>
        /// <param name="sparqlQuery">The query to be executed</param>
        /// <param name="anonymousMembersMap">mappings for anonymous types</param>
        /// <returns>An enumeration over the bound objects</returns>
        /// <remarks>The SPARQL query should be written to return a single variable binding or triples binding.</remarks>
        public IEnumerable<T> ExecuteQuery<T>(string sparqlQuery, List<Tuple<string, string>> anonymousMembersMap = null)
        {
            if (anonymousMembersMap != null)
            {
                return ExecuteQuery<T>(new SparqlQueryContext(sparqlQuery, anonymousMembersMap));
            }
            else
            {
                return ExecuteQuery<T>(new SparqlQueryContext(sparqlQuery));
            }
        }

        /// <summary>
        /// Executes a SPARQL query against the underlying store and binds the results to
        /// domain context objects
        /// </summary>
        /// <typeparam name="T">The type of domain context object to bind to</typeparam>
        /// <param name="sparqlQuery">The query context that contains sparql query to be executed and it's params</param>
        /// <returns></returns>
        public IEnumerable<T> ExecuteQuery<T>(SparqlQueryContext sparqlQuery)
        {
            var bindType = GetImplType(typeof(T));
            var sparqlResult = _store.ExecuteSparql(sparqlQuery);
            if (typeof(BrightstarEntityObject).IsAssignableFrom(bindType))
            {
                return BindSparqlResultToKnownType<T>(sparqlResult, sparqlQuery.OrderingDirections);
            }
            else if (IsAnonymousType(typeof(T)))
            {
                return BindSparqlResultToAnonymousType<T>(sparqlResult, sparqlQuery.AnonymousMembersMap);
            }
            else
            {
                return BindRowsToEntites<T>(sparqlResult);
            }
        }


        /// <summary>
        /// Executes a SPARQL query against the underlying store and binds the results to
        /// domain context objects
        /// </summary>
        /// <typeparam name="T">The type of domain context object to bind to</typeparam>
        /// <param name="sparqlLinqQueryContext">The query to be executed</param>
        /// <returns>An enumeration over the bound objects</returns>
        /// <remarks>The SPARQL query should be written to return a single variable binding or triples binding.</remarks>
        public override IEnumerable<T> ExecuteQuery<T>(SparqlLinqQueryContext sparqlLinqQueryContext)
        {
            var bindType = GetImplType(typeof(T));
            var sparqlResult = _store.ExecuteSparql(sparqlLinqQueryContext);
            if (typeof(BrightstarEntityObject).IsAssignableFrom(bindType))
            {
                return BindSparqlResultToKnownType<T>(sparqlResult, sparqlLinqQueryContext.OrderingDirections);

            }
            else if (IsAnonymousType(typeof(T)))
            {
                return BindSparqlResultToAnonymousType<T>(sparqlResult, sparqlLinqQueryContext.AnonymousMembersMap);
            }
            else if (sparqlLinqQueryContext.HasMemberInitExpression)
            {
                return BindWithLinq<T>(sparqlLinqQueryContext);
            }
            else
            {
                return BindRowsToEntites<T>(sparqlResult);
            }
        }

        private object ConvertString(string value, string lang, Type t)
        {
            var converter = GetStringConverter(t);
            if (converter == null)
            {
                throw new EntityFrameworkException("No SPARQL results conversion found from string to type '{0}'",
                                                   t.FullName);
            }
            return converter(value, lang);
        }

        /// <summary>
        /// Handler for the special case query that selects a specific instance of a type
        /// </summary>
        /// <typeparam name="T">The entity type to create for then instance if it is found</typeparam>
        /// <param name="instanceIdentifier">The identifier for the instance</param>
        /// <param name="typeIdentifier">The identifier for the type that the instance must be an instance of</param>
        /// <returns>An enumerable that returns 0 or 1 instances of <typeparamref name="T"/>. If the resource identified
        /// by <paramref name="instanceIdentifier"/> is an instance of the resource identifier by <paramref name="typeIdentifier"/>,
        /// the enumeration returns a single object, otherwise it returns no objects.</returns>
        public override IEnumerable<T> ExecuteInstanceQuery<T>(string instanceIdentifier, string typeIdentifier)
        {
            var sparqlQuery = String.Format("ASK {0} {{ <{1}> a <{2}>. }}", _store.GetDatasetClause(), instanceIdentifier, typeIdentifier);


            var sparqResult = _store.ExecuteSparql(sparqlQuery);
            var resultDoc = sparqResult.ResultDocument;
            if (resultDoc.SparqlBooleanResult())
            {
                var dataObject = _store.MakeDataObject(instanceIdentifier);
                yield return BindDataObject<T>(dataObject, GetImplType(typeof (T)));
            }
            yield break;
        }

        private class AnonymousConstructorArg
        {
            public string PropertyName;
            public string VariableName;
            public Func<string, string, object> ValueConverter;
            public object DefaultValue;
        }

        
        private Func<string, string, object> GetStringConverter(Type targetType)
        {
            if (typeof(BrightstarEntityObject).IsAssignableFrom(GetImplType(targetType)))
            {
                return (s,l) => BindSingleBrightstarObject(targetType, s);
            }
            if (targetType == typeof (PlainLiteral)) return (s, l) => new PlainLiteral(s, l);
            if (targetType == typeof(string)) return (x,l) => x;
            if (targetType == typeof(bool)) return (x, l) => Convert.ToBoolean(x);
            if (targetType == typeof(int)) return (x, l) => Convert.ToInt32(x);
            if (targetType == typeof(short)) return (x, l) => Convert.ToInt16(x);
            if (targetType == typeof(long)) return (x, l) => Convert.ToInt64(x);
            if (targetType == typeof(DateTime)) return (x, l) => Convert.ToDateTime(x);
            if (targetType == typeof(Byte)) return (x, l) => Convert.ToByte(x);
            if (targetType == typeof(Decimal)) return (x, l) => Convert.ToDecimal(x);
            if (targetType == typeof(Double)) return (x, l) => Convert.ToDouble(x);
            if (targetType == typeof(SByte)) return (x, l) => Convert.ToSByte(x);
            if (targetType == typeof(Single)) return (x, l) => Convert.ToSingle(x);
            if (targetType == typeof(UInt16)) return (x, l) => Convert.ToUInt16(x);
            if (targetType == typeof(UInt32)) return (x, l) => Convert.ToUInt32(x);
            if (targetType == typeof(UInt64)) return (x, l) => Convert.ToUInt64(x);
            var stringConstructor = targetType.GetConstructor(new Type[] {typeof (string)});
            if (stringConstructor != null) return (x, l) => stringConstructor.Invoke(new object[] { x });
            return null;
        }

        private object BindSingleBrightstarObject(Type targetType, string identity)
        {
            if (!Uri.IsWellFormedUriString(identity, UriKind.Absolute))
            {
                throw new EntityFrameworkException("Cannot bind string value '{0}' as an entity resource URI", identity);
            }
            var dataObject = _store.MakeDataObject(identity);
            List<BrightstarEntityObject> trackedObjectList;
            if (_trackedObjects.TryGetValue(dataObject.Identity, out trackedObjectList))
            {
                var trackedObject = trackedObjectList.FirstOrDefault(x => targetType.IsAssignableFrom(x.GetType()));
                if (trackedObject != null) return trackedObject;
            }
            return Activator.CreateInstance(GetImplType(targetType), this, dataObject);
        }

        private static bool IsAnonymousType(Type type)
        {
            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                       && type.IsGenericType && type.Name.Contains("AnonymousType")
                       && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) ||
                           type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                       && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        /// <summary>
        /// Implements the mapping between a domain object's identity string
        /// and the underlying store resource identity.
        /// </summary>
        /// <param name="identifierProperty">The property that provides the identifier</param>
        /// <param name="id">The identity string to be mapped</param>
        /// <returns>The mapped store resource identity</returns>
        /// <remarks>For Brightstar, the domain object's identity string is the
        /// same as the Brightstar resoure identity, so this mapping is an identity mapping.</remarks>
        public override string MapIdToUri(PropertyInfo identifierProperty, string id)
        {
            var entityType = identifierProperty.DeclaringType;
            var prefix = EntityMappingStore.GetIdentifierPrefix(entityType);
            return prefix + Uri.EscapeUriString(id);
        }

        /// <summary>
        /// Deletes an domain context object from the store
        /// </summary>
        /// <param name="objectToDelete">The object to be deleted</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="objectToDelete"/> is not an instance of a class derived from <see cref="BrightstarEntityObject"/>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="objectToDelete"/> is NULL.</exception>
        public override void DeleteObject(object objectToDelete)
        {
            if (objectToDelete == null) throw new ArgumentNullException("objectToDelete");
            var bsObject = objectToDelete as BrightstarEntityObject;
            if (bsObject == null || !bsObject.IsAttached)
            {
                throw new ArgumentException("Object is not attached to a context and cannot be deleted");
            }
            var identity = bsObject.DataObject.Identity;
            _trackedObjects.Remove(identity);
            RemoveReferences(bsObject);
            bsObject.DataObject.Delete();
            bsObject.DataObject = null;
        }

        /// <summary>
        /// Return the RDF datatype to apply to literals of the specified system type.
        /// </summary>
        /// <param name="systemType">The System.Type of the literal value</param>
        /// <returns>The RDF datatype to apply</returns>
        public override string GetDatatype(Type systemType)
        {
            return RdfDatatypes.GetRdfDatatype(systemType);
        }

        /// <summary>
        /// Return the list of graphs to query or null to query the default dataset
        /// </summary>
        /// <returns></returns>
        public override IList<string> GetDataset()
        {
            return _store.GetDataset();
        }

        /// <summary>
        /// This method is invoked when the entity context is being disposed.
        /// </summary>
        protected override void Cleanup()
        {
            _store.Dispose();
        }

        #endregion

        ///<summary>
        /// Creates a new domain context object
        ///</summary>
        ///<typeparam name="T">The type of domain context object to create</typeparam>
        ///<returns>The new object</returns>
        public T CreateObject<T>() where T : class
        {
            var dataObject = CreateDataObject(typeof(T));
            var bindType = GetImplType(typeof (T));

            return ((T)Activator.CreateInstance(bindType, this, dataObject));
        }

        /// <summary>
        /// Creates a new DataObject backing object for a context object of a specific type
        /// </summary>
        /// <param name="domainObjectType">The type of domain context object that the data object backs</param>
        /// <returns>The new data object</returns>
        internal IDataObject CreateDataObject(Type domainObjectType)
        {
            var identifierInfo = EntityMappingStore.GetIdentityInfo(domainObjectType);
            string prefix = identifierInfo == null ? null : identifierInfo.BaseUri;
            var dataObject = identifierInfo != null && identifierInfo.KeyProperties != null
                ? null
                : _store.MakeNewDataObject( String.Empty.Equals(prefix) ? Constants.GeneratedUriPrefix : prefix);
            if (dataObject != null)
            {
                IEnumerable<string> typeIds = EntityMappingStore.MapTypeToUris(domainObjectType);
                foreach (var typeId in typeIds)
                {
                    if (!String.IsNullOrEmpty(typeId))
                    {
                        var typeObject = _store.MakeDataObject(typeId);
                        dataObject.AddProperty(DataObject.TypeDataObject, typeObject);
                    }
                }
            }
            return dataObject;
        }


        private object Bind(IDataObject dataObject, Type t)
        {
            if (dataObject == null) return null;
            var bindType = GetImplType(t);
            List<BrightstarEntityObject> trackedObjectList;
            if (_trackedObjects.TryGetValue(dataObject.Identity, out trackedObjectList))
            {
                var tracked = trackedObjectList.FirstOrDefault(x => t.IsAssignableFrom(x.GetType()));
                if (tracked != null)
                {
                    return tracked;
                }
                /*
                throw new EntityFrameworkException(
                    String.Format(
                        "The data object with identity {0} is already tracked as type {1} and cannot be rebound to the requested type {2}",
                        dataObject.Identity, tracked.GetType().FullName, t.FullName));*/
            }
            return Activator.CreateInstance(bindType, this, dataObject);
        }

        internal T Bind<T>(IDataObject dataObject) where T : class
        {
            return Bind(dataObject, typeof (T)) as T;
        }

        /// <summary>
        /// Returns an enumeration over all of the currently tracked BrightstarEntityObjects
        /// that are bound to the same identity as <paramref name="dataObject"/>
        /// </summary>
        /// <param name="dataObject"></param>
        /// <returns></returns>
        internal IEnumerable<BrightstarEntityObject> GetTrackedObjects(IDataObject dataObject)
        {
            return _trackedObjects.ContainsKey(dataObject.Identity)
                       ? (IEnumerable<BrightstarEntityObject>) _trackedObjects[dataObject.Identity]
                       : new BrightstarEntityObject[0];
        }

        internal BrightstarEntityObject GetTrackedObject(string identity)
        {
            List<BrightstarEntityObject> trackedObjects;
            if (_trackedObjects.TryGetValue(identity, out trackedObjects))
            {
                return trackedObjects.FirstOrDefault();
            }
            return null;
        }

        internal IEnumerable<PropertyInfo> GetArcProperties(Type t, string propertyType)
        {
            return from p in t.GetProperties() 
                   let ph = GetPropertyHint(p) 
                   where ph != null && ph.MappingType == PropertyMappingType.Arc && ph.SchemaTypeUri.Equals(propertyType) 
                   select p;
        }

        internal IEnumerable<PropertyInfo> GetInverseArcProperties(Type t, string propertyType)
        {
            return from p in t.GetProperties()
                   let ph = GetPropertyHint(p)
                   where
                       ph != null && ph.MappingType == PropertyMappingType.InverseArc &&
                       ph.SchemaTypeUri.Equals(propertyType)
                   select p;
        }

        internal bool IsCollectionProperty(PropertyInfo p)
        {
            return typeof (IEnumerable).IsAssignableFrom(p.PropertyType);
        }

        internal Type GetItemType(PropertyInfo p)
        {
            return p.PropertyType.GetGenericArguments().First();
        }

        internal void AddArc(BrightstarEntityObject subj, string propertyType, BrightstarEntityObject obj, bool overwrite)
        {
            if (overwrite)
            {
                subj.DataObject.SetProperty(propertyType, obj.DataObject);
            }
            else
            {
                subj.DataObject.AddProperty(propertyType, obj.DataObject);
            }
            foreach (var srcObject in GetTrackedObjects(subj.DataObject))
            {
                foreach(var srcProperty in GetArcProperties(srcObject.GetType(), propertyType))
                {
                    if (IsCollectionProperty(srcProperty))
                    {
                        Type itemType = GetItemType(srcProperty);
                        if (itemType.IsAssignableFrom(obj.GetType()))
                        {
                            srcObject.UpdatePropertyCollection(srcProperty.Name, obj, null);
                        }
                        else
                        {
                            srcObject.UpdatePropertyCollection(srcProperty.Name, Bind(obj.DataObject, itemType) as BrightstarEntityObject, null);
                        }
                    }
                    else
                    {
                        var existing = srcProperty.GetValue(srcObject, null) as BrightstarEntityObject;
                        if (obj.Equals(existing)) continue;
                        if (srcProperty.PropertyType.Equals(obj.GetType()))
                        {
                            srcObject.UpdateProperty(srcProperty.Name, obj);
                        } else
                        {
                            srcObject.UpdateProperty(srcProperty.Name, Bind(obj.DataObject, srcProperty.PropertyType) as BrightstarEntityObject);
                        }
                    }
                }
            }
            foreach (var destObject in GetTrackedObjects(obj.DataObject))
            {
                foreach (var destProperty in GetInverseArcProperties(destObject.GetType(), propertyType))
                {
                    if (IsCollectionProperty(destProperty))
                    {
                        Type itemType = GetItemType(destProperty);
                        if (itemType.IsAssignableFrom(subj.GetType()))
                        {
                            destObject.UpdatePropertyCollection(destProperty.Name, subj, null);
                        }
                            /* ISSUE #125 - Don't force type coercion when setting inverse properties
                             * Only update if the data object can be bound to the type of the inverse property
                        else
                        {
                            destObject.UpdatePropertyCollection(destProperty.Name,
                                                                Bind(subj.DataObject, itemType) as
                                                                BrightstarEntityObject, null);
                        }
                             */
                        else
                        {
                            var itemTypeUri = MapTypeToUri(itemType);
                            if (subj.DataObject.GetTypes().Contains(itemTypeUri))
                            {
                                destObject.UpdatePropertyCollection(
                                    destProperty.Name,
                                    Bind(subj.DataObject, itemType) as BrightstarEntityObject,
                                    null);
                            }
                        }
                    }
                    else
                    {
                        var existing = destProperty.GetValue(destObject, null) as BrightstarEntityObject;
                        if (existing != null && subj.Equals(existing)) continue;
                        if (destProperty.PropertyType.IsAssignableFrom(subj.GetType()))
                        {
                            destObject.UpdateProperty(destProperty.Name, subj);
                        }
                            /* ISSUE #125 - Don't force type coercion when setting inverse properties
                             * Only update if the data object can be bound to the type of the inverse property
                        else
                        {
                            destObject.UpdateProperty(destProperty.Name,
                                                      Bind(subj.DataObject, destProperty.PropertyType) as
                                                      BrightstarEntityObject);
                        }
                             */
                        else
                        {
                            var itemTypeUri = MapTypeToUri(destProperty.PropertyType);
                            if (subj.DataObject.GetTypes().Contains(itemTypeUri))
                            {
                                destObject.UpdateProperty(
                                    destProperty.Name,
                                    Bind(subj.DataObject, destProperty.PropertyType) as BrightstarEntityObject);
                            }
                        }
                    }
                }
            }
        }

        internal void RemoveArc(IDataObject subj, string propertyType, IDataObject obj)
        {
            subj.RemoveProperty(propertyType, obj);
            foreach(var srcObject in GetTrackedObjects(subj))
            {
                foreach(var srcProperty in GetArcProperties(srcObject.GetType(), propertyType))
                {
                    if (IsCollectionProperty(srcProperty))
                    {
                        srcObject.UpdatePropertyCollection(srcProperty.Name, null, obj.Identity);
                    }
                    else
                    {
                        srcObject.UpdateProperty(srcProperty.Name, null);
                    }
                }
            }
            foreach (var destObject in GetTrackedObjects(obj))
            {
                foreach (var destProperty in GetInverseArcProperties(destObject.GetType(), propertyType))
                {
                    if (IsCollectionProperty(destProperty))
                    {
                        destObject.UpdatePropertyCollection(destProperty.Name, null, obj.Identity);
                    }
                    else
                    {
                        destObject.UpdateProperty(destProperty.Name, null);
                    }
                }
            }
        }

        /// <summary>
        /// Removes any reference to <paramref name="toRemove"/> from locally 
        /// tracked objects in this context.
        /// </summary>
        /// <param name="toRemove">The entity object that is to be removed</param>
        /// <remarks>This method is used when <paramref name="toRemove"/> has been locally deleted
        /// in the context.</remarks>
        private void RemoveReferences(BrightstarEntityObject toRemove)
        {
            foreach (var trackedList in _trackedObjects.Values)
            {
                foreach (var tracked in trackedList)
                {
                    tracked.RemoveReferences(toRemove);
                }
            }
        }

        /// <summary>
        /// Rebinds a resource to a new entity type
        /// </summary>
        /// <typeparam name="T">The new entity type to bind to</typeparam>
        /// <param name="beo">The existing entity object to be rebound</param>
        /// <returns>An instance of <typeparamref name="T"/> that is bound to the same underlying resource as <paramref name="beo"/>.</returns>
        public T Become<T>(BrightstarEntityObject beo)
        {
            if (!EntityMappingStore.IsKnownInterface(typeof(T)))
            {
                throw new MappingNotFoundException(typeof(T));
            }
            var implType = EntityMappingStore.GetImplType(typeof (T));
            List<BrightstarEntityObject> trackedObjects;
            if (_trackedObjects.TryGetValue(beo.DataObject.Identity, out trackedObjects))
            {
                var ret = trackedObjects.OfType<T>().FirstOrDefault();
                if (ret != null) return ret;
            }
            var dataObject = beo.DataObject;
            foreach(var typeUri in EntityMappingStore.MapTypeToUris(implType))
            {
                var typeDo = GetDataObject(new Uri(typeUri), false);
                dataObject.AddProperty(DataObject.TypeDataObject, typeDo);
            }
            return (T) Activator.CreateInstance(implType, this, beo.DataObject);
        }

        /// <summary>
        /// Removes a type identifier from a resource
        /// </summary>
        /// <typeparam name="T">The entity type whose type identifier is to be removed</typeparam>
        /// <param name="beo">An existing entity bound to the resource to be updated</param>
        public void Unbecome<T>(BrightstarEntityObject beo) 
        {
            if (!EntityMappingStore.IsKnownInterface(typeof(T)))
            {
                throw new MappingNotFoundException(typeof(T));
            }
            var typeUri = EntityMappingStore.GetMappedInterfaceTypeUri(EntityMappingStore.GetImplType(typeof (T)));
            if (!String.IsNullOrEmpty(typeUri))
            {
                var typeDo = GetDataObject(new Uri(typeUri), false);
                beo.DataObject.RemoveProperty(DataObject.TypeDataObject, typeDo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="typeUris"></param>
        internal void EnforceClassUniqueConstraint(string identity, IEnumerable<string> typeUris)
        {
            foreach (var t in typeUris)
            {
                _store.AddPrecondition(false, identity, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", t);
            }
        }
    }
}
