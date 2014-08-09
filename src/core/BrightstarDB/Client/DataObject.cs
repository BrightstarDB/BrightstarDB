using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.EntityFramework;
using BrightstarDB.Model;
using BrightstarDB.Rdf;

#if SILVERLIGHT || PORTABLE
using VDS=VDS.RDF;
#endif
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Client
{
    /// <summary>
    /// DataObject is the generic data object that contains a collection of properties.
    /// Properties can be added and removed. All changes occur in a client context. When the 
    /// context changes are saved they are sent back to the server.
    /// </summary>
    internal class DataObject : IDataObject
    {
        ///<summary>
        /// The CURIE for identity of rdf:nil data object. 
        ///</summary>
        public const string RdfNil = "rdf:nil";

        /// <summary>
        /// The CURIE for the data object type rddf:List
        /// </summary>
        public const string RdfList = "rdf:List";

        /// <summary>
        /// The CURIE for property type rdf:first
        /// </summary>
        public const string RdfFirst = "rdf:first";

        /// <summary>
        /// The CURIE for property type rdf:rest
        /// </summary>
        public const string RdfRest = "rdf:rest";

        /// <summary>
        /// A DataObject that represents the RDF type property http://www.w3.org/1999/02/22-rdf-syntax-ns#type
        /// </summary>
        public static readonly IDataObject TypeDataObject = new DataObject(null, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

        /// <summary>
        /// DataObject identity
        /// </summary>
        private readonly string _identity;

        /// <summary>
        /// The store context for this object
        /// </summary>
        private readonly IInternalDataObjectStore _store;

        /// <summary>
        /// Indicates if the state of the object has been retreived from the server.
        /// </summary>
        private bool _isLoaded;

        /// <summary>
        /// The state of this data object is represented as a collection of triples
        /// </summary>
        private List<Triple> _triples;

        /// <summary>
        /// Indicates if this entity is new
        /// </summary>
        private bool _isNew;

        internal DataObject(IInternalDataObjectStore store)
        {
            _store = store;
            _identity = Constants.GeneratedUriPrefix + Guid.NewGuid();
            _triples = new List<Triple>();
            _isLoaded = true;
            _isNew = true;
        }

        internal DataObject(IInternalDataObjectStore store, string identity, bool isNew = false)
        {
            _store = store;
            _triples = new List<Triple>();
            _identity = identity;
            _isLoaded = isNew;
            _isNew = isNew;
        }

        /// <summary>
        /// Flag indicating if this data object is new or not.
        /// </summary>
        public bool IsNew
        {
            get { return _isNew; }
            internal set { _isNew = value; }
        }

        /// <summary>
        /// Determines if this data object has one or more changes applied to it
        /// </summary>
        /// <returns>True if a property has been modified, added to or deleted from this <see cref="DataObject"/>, false otherwise.</returns>
        public bool IsModified
        {
            get
            {
                return _store.AddTriples.Any(x => x.Subject.Equals(Identity)) ||
                       _store.DeletePatterns.Any(x => x.Subject.Equals(Identity));
            }
        }
        /// <summary>
        /// Return flag that indicates if the triples for this data object have already been retrieved.
        /// </summary>
        public bool IsLoaded
        {
            get { return _isLoaded; }
        }

        /// <summary>
        /// The current state of this dataobject
        /// </summary>
        public IEnumerable<Triple> Triples
        {
            get { return _triples; }
        }

        #region Implementation of IDataObject

        /// <summary>
        /// The identity of the resource that this data object wraps.
        /// </summary>
        /// <returns>The identity of the resource</returns>
        public string Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Sets the type of this data object
        /// </summary>
        /// <param name="type">The new data object type</param>
        /// <returns>This IDataObject to allow chained calls</returns>
        public IDataObject SetType(IDataObject type)
        {
            SetRelatedObject(TypeDataObject, type);
            return this;
        }

        /// <summary>
        /// Gets the uri types of this data object
        /// </summary>
        /// <returns>A list of uri types</returns>
        public IList<string> GetTypes()
        {
            CheckLoaded();
            return Triples.Where(t => t.Predicate == TypeDataObject.Identity).Select(t => t.Object).ToList();
        }


        /// <summary>
        /// Sets the property of this object to the specified value
        /// </summary>
        /// <param name="type">The type of the property to set</param>
        /// <param name="value">The new value of the property</param>
        /// <param name="langCode">OPTIONAL : the language code for the new literal. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataObject to allow chained calls</returns>
        /// <remarks>This method will remove all existing properties of type <paramref name="type"/> from this data object
        /// and add a single replacement property of the same type with <paramref name="value"/> as the property value.</remarks>
        public IDataObject SetProperty(IDataObject type, object value, string langCode = null)
        {
            if (value is IDataObject) return SetRelatedObject(type, value as IDataObject);
            if (value is Uri) return SetRelatedObject(type, _store.MakeDataObject(value.ToString()));
            if (type == null) throw new ArgumentNullException("type");
            if (value == null) throw new ArgumentNullException("value");
            string dataType = RdfDatatypes.GetRdfDatatype(value.GetType());
            string litString = RdfDatatypes.GetLiteralString(value);
            SetPropertyLiteral(type, litString, dataType, langCode);
            return this;
        }

        /// <summary>
        /// Sets the property of this object to the specified value
        /// </summary>
        /// <param name="type">The type of the property to set as a CURIE or URI string</param>
        /// <param name="value">The new value of the property</param>
        /// <param name="langCode">OPTIONAL: The language code for the new literal. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataObject to allow chained calls</returns>
        /// <remarks>This method will remove all existing properties of type <paramref name="type"/> from this data object
        /// and add a single replacement property of the same type with <paramref name="value"/> as the property value.</remarks>
        public IDataObject SetProperty(string type, object value, string langCode = null)
        {
            return SetProperty(_store.MakeDataObject(type), value, langCode);
        }

        /// <summary>
        /// Adds a new property value to this object
        /// </summary>
        /// <param name="type">The type of the property to add</param>
        /// <param name="value">The value of the property</param>
        /// <param name="lang">OPTIONAL: The language code of the literal value. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataOjbect to allow chained calls</returns>
        public IDataObject AddProperty(IDataObject type, object value, string lang = null)
        {
            if (value is IDataObject)
            {
                AddDataObjectProperty(type, value as IDataObject);
            }
            else if (value is Uri)
            {
                AddDataObjectProperty(type, _store.MakeDataObject(value.ToString()));
            }
            else
            {
                if (type == null) throw new ArgumentNullException("type");
                if (value == null) throw new ArgumentNullException("value");
                string dataType = RdfDatatypes.GetRdfDatatype(value.GetType());
                string litString = RdfDatatypes.GetLiteralString(value);
                AddLiteralProperty(type, litString, dataType, lang ?? RdfDatatypes.GetLiteralLanguageTag(value));
            }
            return this;
        }

        /// <summary>
        /// Adds a new property value to this object
        /// </summary>
        /// <param name="type">The type of the property to add as a CURIE or URI string</param>
        /// <param name="value">The value of the property</param>
        /// <param name="lang">OPTIONAL: The language code of the literal value. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        /// <returns>This IDataOjbect to allow chained calls</returns>
        public IDataObject AddProperty(string type, object value, string lang = null)
        {
            AddProperty(_store.MakeDataObject(type), value, lang);
            return this;
        }

        ///<summary>
        /// Removes any property on this data object with the specified type and value
        ///</summary>
        ///<param name="type">The type of the property to be removed as a URI or CURIE</param>
        ///<param name="value">The value of the property to be removed</param>
        ///<param name="lang">OPTIONAL: The language code of the property to be removed. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        ///<returns>This IDataObject to allow chained calls</returns>
        ///<remarks>If this object has no matching property, then this call is a no-op</remarks>
        public IDataObject RemoveProperty(string type, object value, string lang = null)
        {
            RemoveProperty(_store.MakeDataObject(type), value, lang);
            return this;
        }

        ///<summary>
        /// Removes any property on this data object with the specified type and value
        ///</summary>
        ///<param name="type">The type of the property to be removed</param>
        ///<param name="value">The value of the property to be removed</param>
        ///<param name="lang">OPTIONAL: The language code of the property to be removed. This parameter is ignored if <paramref name="value"/> is an <see cref="IDataObject"/></param>
        ///<returns>This IDataObject to allow chained calls</returns>
        ///<remarks>If this object has no matching property, then this call is a no-op</remarks>
        public IDataObject RemoveProperty(IDataObject type, object value, string lang = null)
        {
            if (value is IDataObject)
            {
                RemoveDataObjectProperty(type, value as IDataObject);
            }
            else if (value is Uri)
            {
                RemoveDataObjectProperty(type, _store.MakeDataObject(value.ToString()));
            }
            else
            {
                if (type == null) throw new ArgumentNullException("type");
                if (value == null) throw new ArgumentNullException("value");
                string dataType = RdfDatatypes.GetRdfDatatype(value.GetType());
                string litString = RdfDatatypes.GetLiteralString(value);
                RemoveLiteralProperty(type, litString, dataType, lang);
            }
            return this;
        }

        ///<summary>
        /// Removes all properties of the specified type from this data object
        ///</summary>
        ///<param name="type">The type of the properties to be removed as a URI or CURIE</param>
        ///<returns>This IDataObject to allow chained calls</returns>
        public IDataObject RemovePropertiesOfType(string type)
        {
            return RemovePropertiesOfType(_store.MakeDataObject(type));
        }

        ///<summary>
        /// Removes all properties of the specified type from this data object
        ///</summary>
        ///<param name="type">The type of the properties to be removed</param>
        ///<returns>This IDataObject to allow chained calls</returns>
        public IDataObject RemovePropertiesOfType(IDataObject type)
        {
            CheckLoaded();

            // remove matching triples
            //IEnumerable<Triple> matchingTriples = _triples.Where(t => t.Predicate.Equals(type.Identity));
            //_store.DeletePatterns.AddRange(matchingTriples);
            if (!_store.DeletePatterns.Any(t=>t.Subject.Equals(Identity) && t.Predicate.Equals(type.Identity) && t.Object.Equals(Constants.WildcardUri)))
            {
                AddDeleteTriples(new Triple
                    {
                        Subject = Identity,
                        Predicate = type.Identity,
                        Object = Constants.WildcardUri
                    });
            }
            _triples.RemoveAll(t => t.Predicate.Equals(type.Identity));
            _store.AddTriples.RemoveAll(t => t.Predicate.Equals(type.Identity) && t.Subject.Equals(Identity));
            return this;
        }

        /// <summary>
        /// Removes properties of the specified type where this data object is the value
        /// </summary>
        /// <param name="type">The type of the properties to be removed as a URI or CURIE</param>
        /// <returns>This IDataObject to allow chained calls</returns>
        public IDataObject RemoveInversePropertiesOfType(string type)
        {
            return RemoveInversePropertiesOfType(_store.MakeDataObject(type));
        }

        /// <summary>
        /// Removes properties of the specified type where this data object is the value
        /// </summary>
        /// <param name="type">The type of the properties to be removed</param>
        /// <returns>This IDataObject to allow chained calls</returns>
        public IDataObject RemoveInversePropertiesOfType(IDataObject type)
        {
            CheckLoaded();
            AddDeleteTriples(new Triple{Subject = Constants.WildcardUri, Predicate = type.Identity, Object = Identity, IsLiteral = false});
            _store.AddTriples.RemoveAll(t => t.Predicate.Equals(type.Identity) && t.Object.Equals(Identity));
            return this;
        }

        ///<summary>
        /// Retrieves the value of the property of this data object with the specified property type
        ///</summary>
        ///<param name="type">The property type as a URI or CURIE</param>
        ///<returns>The value of the first property of the specified type or null if no match was found</returns>
        public object GetPropertyValue(string type)
        {
            return GetPropertyValue(_store.MakeDataObject(type));
        }

        ///<summary>
        /// Retrieves the value of the property of this data object with the specified property type
        ///</summary>
        ///<param name="type">The property type</param>
        ///<returns>The value of the first property of the specified type or null if no match was found</returns>
        public object GetPropertyValue(IDataObject type)
        {
            CheckLoaded();
            Triple triple = _triples.FirstOrDefault(t => t.Predicate.Equals(type.Identity.ToString()));
            return triple != null ? CreateTypedObject(triple) : null;
        }

        /// <summary>
        /// Retrieves the values of all properties of this data object with the specified property type
        /// </summary>
        /// <param name="type">The property type as a URI or CURIE</param>
        /// <returns>An enumeration of the values of all properties of the specified type.</returns>
        public IEnumerable<object> GetPropertyValues(string type)
        {
            return GetPropertyValues(_store.MakeDataObject(type));
        }

        /// <summary>
        /// Retrieves the values of all properties of this data object with the specified property type
        /// </summary>
        /// <param name="type">The property type</param>
        /// <returns>An enumeration of the values of all properties of the specified type.</returns>
        public IEnumerable<object> GetPropertyValues(IDataObject type)
        {
            CheckLoaded();
            return _triples.Where(t => t.Predicate.Equals(type.Identity)).Select(CreateTypedObject);
        }

        ///<summary>
        /// Returns all data objects that have a property of the specified type where
        /// the property value is this data object
        ///</summary>
        ///<param name="type">The property type</param>
        ///<returns>An enumeration of data object values</returns>
        public IEnumerable<IDataObject> GetInverseOf(IDataObject type)
        {
            return _store.GetInverseOf(this, type);
        }

        /// <summary>
        /// Get the data objects that reference this object with a property of the specified type.
        /// </summary>
        /// <param name="type">The uri or curi of the referencing property type</param>
        /// <returns>An enumeration of the referenced data objects</returns>
        public IEnumerable<IDataObject> GetInverseOf(string type)
        {
            return GetInverseOf(_store.MakeDataObject(type));
        }

        /// <summary>
        /// Delete this data object from the store.
        /// </summary>
        public void Delete()
        {
            CheckLoaded();

            // add all triples to txn remove
            _store.DeletePatterns.AddRange(_triples);

            // remove all add triples for this DataObject or references to it.
            _store.AddTriples.RemoveAll(t => t.Subject.Equals(Identity));
            _store.AddTriples.RemoveAll(t => t.Object.Equals(Identity));
            
            // delete triples where this DataObject is the object
            AddDeleteTriples(new Triple {Subject = Constants.WildcardUri, Predicate = Constants.WildcardUri, Object = Identity});

            // remove all triples from current state
            _triples.Clear();
        }

        /// <summary>
        /// Change the URI identifier for this data object.
        /// </summary>
        /// <remarks>This change will update all triples where the data object identity
        /// is the subject or object. It will not change predicates.</remarks>
        /// <param name="newIdentity">The new URI identifier</param>
        /// <param name="enforceClassUniqueConstraint">Add an update precondition to ensure that the update will fail if the store already
        /// contains an RDF resource with the same rdf:type(s) as this data object.</param>
        public IDataObject UpdateIdentity(string newIdentity, bool enforceClassUniqueConstraint)
        {
            if (newIdentity == null) throw new ArgumentNullException("newIdentity", "DataObject Identity must not be null");
            if (String.IsNullOrWhiteSpace(newIdentity)) throw new ArgumentException("DataObject Identity must not be an empty string or whitespace.", "newIdentity");
            if (newIdentity.Equals(Identity))
            {
                // No change
                return this;
            }

            if (IsNew)
            {
                // Simple case - we only have to change the uncommitted triples locally.
                CheckLoaded();
                var ret = new DataObject(_store, newIdentity, true);
                ret.BindTriples(_triples.Select(t => ReplaceIdentity(t, newIdentity)), true, enforceClassUniqueConstraint);
                Delete();
                return ret;
            }
            else
            {
                CheckLoaded();
                var ret = new DataObject(_store, newIdentity, true);
                ret.BindTriples(_triples.Union(_store.GetReferencingTriples(this)).Select(t => ReplaceIdentity(t, newIdentity)), true, enforceClassUniqueConstraint);
                Delete();
                return ret;
            }
        }

        #endregion

        private Triple ReplaceIdentity(Triple t, string newIdentity)
        {
            return new Triple
                {
                    Subject = t.Subject.Equals(_identity) ? newIdentity : t.Subject,
                    Predicate = t.Predicate,
                    IsLiteral = t.IsLiteral,
                    Object = t.Object.Equals(_identity) && !t.IsLiteral ? newIdentity : t.Object,
                    DataType = t.DataType,
                    LangCode = t.LangCode,
                    Graph = t.Graph
                };
        }
        /// <summary>
        /// Sets the property of this object to the specified data object
        /// </summary>
        /// <param name="type">The type of the property to set</param>
        /// <param name="value">The new value of the property</param>
        /// <returns>This IDataObject to allow chained calls</returns>
        /// <remarks>This method will remove all existing properties of type <paramref name="type"/> from this data object
        /// and add a single replacement property of the same type with <paramref name="value"/> as the property value.</remarks>
        private IDataObject SetRelatedObject(IDataObject type, IDataObject value)
        {
            CheckLoaded();

            // create a new value triple
            var triple = new Triple
                             {
                                 Subject = Identity,
                                 Predicate = type.Identity,
                                 Object = value.Identity,
                                 IsLiteral = false,
                                 Graph = _store.UpdateGraphUri
                             };

            // use common method for updating local state and the txn
            SetTriple(triple);

            // return this DataObject
            return this;
        }

        /// <summary>
        /// Maps the xsd data type to a .NET type for literal values or creates / looks up a DataObject for 
        /// resources. BNodes get mapped to DataObjects.
        /// </summary>
        /// <param name="triple"></param>
        /// <returns></returns>
        private object CreateTypedObject(Triple triple)
        {
            if (triple.IsLiteral)
            {
                object retValue;
                if (RdfDatatypes.TryParseLiteralString(triple.Object, triple.DataType, triple.LangCode, out retValue))
                {
                    return retValue;
                }
                return triple.Object;
            }
            return _store.MakeDataObject(triple.Object);
        }

        private void SetPropertyLiteral(IDataObject type, string value, string dataType, string langCode = null)
        {
            CheckLoaded();

            if (type.Identity.Equals(Constants.VersionPredicateUri))
            {
                // Update of version property has slightly different handling due to different target graph
                var triple = new Triple
                    {
                        Subject = Identity,
                        Predicate = Constants.VersionPredicateUri,
                        IsLiteral = true,
                        Object = value,
                        DataType = dataType,
                        LangCode = langCode,
                        Graph = _store.VersionGraphUri
                    };
                SetVersionTriple(triple);
            }
            else
            {
                // create a new value triple
                var triple = new Triple
                    {
                        Subject = Identity,
                        Predicate = type.Identity,
                        IsLiteral = true,
                        Object = value,
                        DataType = dataType,
                        LangCode = langCode,
                        Graph = _store.UpdateGraphUri
                    };

                // use common method for updating local state and the txn
                SetTriple(triple);
            }
            // return this DataObject
            return;
        }

        private void SetTriple(Triple triple)
        {
            // see if there are any triples for this property
            bool haveMatch = _triples.Any(t => t.Predicate.Equals(triple.Predicate));
            if (haveMatch)
            {
                // remove all triples in current state that match the predicate
                _triples.RemoveAll(t => t.Predicate.Equals(triple.Predicate));

                // remove any existing property triple in the add triples collection
                _store.AddTriples.RemoveAll(t => t.Subject.Equals(triple.Subject) && t.Predicate.Equals(triple.Predicate));
            }

            // Because this is a set, we use a wildcard to delete any existing properties with the same predicate
            if (!_isNew && !_store.DeletePatterns.Any(t=>t.Subject.Equals(triple.Subject) && t.Predicate.Equals(triple.Predicate) && t.Object.Equals(Constants.WildcardUri)))
            {
                AddDeleteTriples(new Triple
                    {
                        Subject = triple.Subject,
                        Predicate = triple.Predicate,
                        Object = Constants.WildcardUri
                    });
            }

            // add new triple to current triples
            _triples.Add(triple);

            // add new triple to txn add triples
            _store.AddTriples.Add(triple);
        }

        private void SetVersionTriple(Triple triple)
        {
            // see if there are any triples for this property
            bool haveMatch = _triples.Any(t => t.Predicate.Equals(triple.Predicate));
            if (haveMatch)
            {
                // remove all triples in current state that match the predicate
                _triples.RemoveAll(t => t.Predicate.Equals(triple.Predicate));

                // remove any existing property triple in the add triples collection
                _store.AddTriples.RemoveAll(t => t.Subject.Equals(triple.Subject) && t.Predicate.Equals(triple.Predicate));
            }

            // Because this is a set, we use a wildcard to delete any existing properties with the same predicate
            if (!_isNew && !_store.DeletePatterns.Any(t => t.Subject.Equals(triple.Subject) && t.Predicate.Equals(triple.Predicate) && t.Object.Equals(Constants.WildcardUri)))
            {
                _store.DeletePatterns.Add(new Triple
                {
                    Subject = triple.Subject,
                    Predicate = triple.Predicate,
                    Object = Constants.WildcardUri,
                    Graph = _store.VersionGraphUri
                });
            }

            // add new triple to current triples
            _triples.Add(triple);

            // add new triple to txn add triples
            _store.AddTriples.Add(triple);
        }

        private void AddDataObjectProperty(IDataObject type, IDataObject value)
        {
            CheckLoaded();

            var triple = new Triple
                             {
                                 Graph = _store.UpdateGraphUri,
                                 IsLiteral = false,
                                 Object = value.Identity,
                                 Predicate = type.Identity,
                                 Subject = Identity
                             };

            // add to DataObject state triples
            _triples.Add(triple);

            // add to txn
            _store.AddTriples.Add(triple);
        }

        private void AddLiteralProperty(IDataObject type, string value, string dataType, string langCode = null)
        {
            CheckLoaded();

            var triple = new Triple
                             {
                                 Graph = _store.UpdateGraphUri,
                                 IsLiteral = true,
                                 Object = value,
                                 DataType = dataType,
                                 LangCode = langCode,
                                 Predicate = type.Identity,
                                 Subject = Identity
                             };

            // add to DataObject state triples
            _triples.Add(triple);

            // add to txn
            _store.AddTriples.Add(triple);
        }

        private void RemoveDataObjectProperty(IDataObject type, IDataObject value)
        {
            CheckLoaded();

            // remove if in current state
            if (_triples.RemoveAll(t => t.Predicate.Equals(type.Identity) && t.Object.Equals(value.Identity)) > 0)
            {
                // add the triple into the delete txn
                AddDeleteTriples(new Triple
                {
                    Subject = Identity,
                    Predicate = type.Identity,
                    Object = value.Identity,
                    IsLiteral = false
                });
                
            }

            // remove any matches from the list of triples to be added
            _store.AddTriples.RemoveAll(t => t.Subject.Equals(Identity) && t.Predicate.Equals(type.Identity) && t.Object.Equals(value.Identity));
        }

        private void RemoveLiteralProperty(IDataObject type, string value, string dataType, string langCode = null)
        {
            CheckLoaded();

            // remove if in current state
            if (_triples.RemoveAll(t => t.Predicate.Equals(type.Identity) && t.Object.Equals(value)) > 0)
            {
                // add the triple into the delete txn
                AddDeleteTriples(new Triple
                    {
                        Subject = Identity,
                        Predicate = type.Identity,
                        IsLiteral = true,
                        Object = value,
                        DataType = dataType,
                        LangCode = langCode
                    });
            }

            // remove from add txn
            _store.AddTriples.RemoveAll(t => t.Subject.Equals(Identity) && t.Predicate.Equals(type.Identity) && t.Object.Equals(value) && t.DataType.Equals(dataType) && (t.LangCode == null || t.LangCode.Equals(langCode)));
        }

        /// <summary>
        /// Adds one or more delete patterns for the specified base triple
        /// </summary>
        /// <param name="baseDeleteTriple">The base triple match</param>
        /// <remarks>This method ensures that delete patterns target only the
        /// graphs that are read from and the update graph. 
        /// If the store has a defined data set, then a delete pattern is created for
        /// each graph in the data set plus the update graph. Otherwise a single delete pattern
        /// is created with a wildcard graph specifier.</remarks>
        private void AddDeleteTriples(Triple baseDeleteTriple)
        {
            if (_store.DataSetGraphUris == null)
            {
                // Add a pattern to remove matching triple from all graphs
                _store.DeletePatterns.Add(new Triple
                {
                    Subject = baseDeleteTriple.Subject,
                    Predicate = baseDeleteTriple.Predicate,
                    Object = baseDeleteTriple.Object,
                    IsLiteral = baseDeleteTriple.IsLiteral,
                    DataType = baseDeleteTriple.DataType,
                    LangCode = baseDeleteTriple.LangCode,
                    Graph = Constants.WildcardUri
                });
            }
            else
            {
                // Add patterns to remove matching triple from update graph and data set graphs
                _store.DeletePatterns.Add(new Triple
                {
                    Subject = baseDeleteTriple.Subject,
                    Predicate = baseDeleteTriple.Predicate,
                    Object = baseDeleteTriple.Object,
                    IsLiteral = baseDeleteTriple.IsLiteral,
                    DataType = baseDeleteTriple.DataType,
                    LangCode = baseDeleteTriple.LangCode,
                    Graph = _store.UpdateGraphUri
                });
                foreach (var g in _store.DataSetGraphUris)
                {
                    _store.DeletePatterns.Add(new Triple
                    {
                        Subject = baseDeleteTriple.Subject,
                        Predicate = baseDeleteTriple.Predicate,
                        Object = baseDeleteTriple.Object,
                        IsLiteral = baseDeleteTriple.IsLiteral,
                        DataType = baseDeleteTriple.DataType,
                        LangCode = baseDeleteTriple.LangCode,
                        Graph = g
                    });
                }
            }
        }

        /// <summary>
        /// Set the current state of this dataobject to be the set of triples provided.
        /// </summary>
        /// <param name="triples">Dataobject state</param>
        /// <param name="asNewTriples">Also add the triples to the list of new triples on the context store</param>
        /// <param name="enforceClassUniqueConstraint">Add update preconditions to ensure that the update fails if the store already contains
        /// a resource with the same identity and the same rdf:type(s) as this data object.</param>
        internal bool BindTriples(IEnumerable<Triple> triples, bool asNewTriples = false, bool enforceClassUniqueConstraint = false)
        {
            if (_isLoaded)
            {
                // We are rebinding an existing data object due to a refresh
                var updateTriples = triples.ToList();
                if (updateTriples.Count == 0)
                {
                    // The store has no triples for this subject
                    return false;
                }
                _triples = updateTriples;
                if (asNewTriples)
                {
                    _store.AddTriples.AddRange(_triples);
                    if (enforceClassUniqueConstraint)
                    {
                        _store.SetClassUniqueConstraints(
                            Identity,
                            _triples.Where(
                                x => x.Predicate.Equals(TypeDataObject.Identity) && x.Subject.Equals(Identity)).Select(x=>x.Object));
                    }
                }
                return true;
            }
            _triples = triples.ToList();
            if (asNewTriples)
            {
                _store.AddTriples.AddRange(_triples);
                if (enforceClassUniqueConstraint)
                {
                    _store.SetClassUniqueConstraints(
                        Identity,
                        _triples.Where(
                            x => x.Predicate.Equals(TypeDataObject.Identity) && x.Subject.Equals(Identity))
                            .Select(x=>x.Object));
                }
            }
            _isLoaded = true;
            return true;
        }

        private void CheckLoaded()
        {
            if (!_isLoaded)
            {
                _store.BindDataObject(this);
                _isLoaded = true;
            }
        }
    }
}