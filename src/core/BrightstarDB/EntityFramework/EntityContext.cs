using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BrightstarDB.EntityFramework.Query;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The base class for an EntityFramework context
    /// </summary>
    public abstract class EntityContext : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="EntityMappingStore"/> for this entity context
        /// </summary>
        public EntityMappingStore Mappings { get; private set; }

        private bool _disposed;

        /// <summary>
        /// Constructor for an EntityContext object
        /// </summary>
        /// <param name="mappings">The store providing type and property mappings to use in LINQ to SPARQL queries</param>
        protected EntityContext(EntityMappingStore mappings)
        {
            Mappings = mappings;
        }

        /// <summary>
        /// Constructor for an EntityContext object that uses the <see cref="ReflectionMappingProvider"/>
        /// to populate its entity type and property mappings.
        /// </summary>
        /// <remarks>This method is provided so that classes derived from <see cref="EntityContext"/>
        /// can be defined with additional properties that provide access to the types of entities that the context manages</remarks>
        protected EntityContext()
        {
            Mappings = new EntityMappingStore();
            var rmp = new ReflectionMappingProvider();
            rmp.AddMappingsForContext(Mappings, this);
        }

        /// <summary>
        /// Commit local changes to the underlying store
        /// </summary>
        public abstract void SaveChanges();

        /// <summary>
        /// Updates a single object in the object context with data from the data source
        /// </summary>
        /// <param name="mode">A <see cref="RefreshMode"/> value that indicates whether property changes
        /// in the object context are overwritten with property changes from the data source</param>
        /// <param name="entity">The object to be refreshed</param>
        public abstract void Refresh(RefreshMode mode, object entity);

        /// <summary>
        /// Update a collection of objects in the object context with data from the data source
        /// </summary>
        /// <param name="mode">A <see cref="RefreshMode"/> value that indicates whether property changes
        /// in the object context are overwritten with property changes from the data source</param>
        /// <param name="entities">The objects to be refreshed</param>
        public abstract void Refresh(RefreshMode mode, IEnumerable entities);

        /// <summary>
        /// Method invoked to execute a SPARQL query against the underlying store
        /// </summary>
        /// <param name="sparqlQuery">The query to execute</param>
        /// <returns></returns>
        public abstract XDocument ExecuteQuery(string sparqlQuery);

        /// <summary>
        /// Method to execute a SPARQL query against the underlying store and bind its results to instances of a class
        /// </summary>
        /// <typeparam name="T">The type that the SPARQL query results are to be bound to</typeparam>
        /// <param name="sparqlQueryContext">The context object that specifies the SPARQL query and constructor and member mappings</param>
        /// <returns>An enumeration over the bound result objects</returns>
        public abstract IEnumerable<T> ExecuteQuery<T>(SparqlQueryContext sparqlQueryContext);

        /// <summary>
        /// Handler for the special case query that selects a specific instance of a type
        /// </summary>
        /// <typeparam name="T">The entity type to create for then instance if it is found</typeparam>
        /// <param name="instanceIdentifier">The identifier for the instance</param>
        /// <param name="typeIdentifier">The identifier for the type that the instance must be an instance of</param>
        /// <returns>An enumerable that returns 0 or 1 instances of <typeparamref name="T"/>. If the resource identified
        /// by <paramref name="instanceIdentifier"/> is an instance of the resource identifier by <paramref name="typeIdentifier"/>,
        /// the enumeration returns a single object, otherwise it returns no objects.</returns>
        public abstract IEnumerable<T> ExecuteInstanceQuery<T>(string instanceIdentifier, string typeIdentifier);
 
        /// <summary>
        /// Returns the property hint for the specified .NET property
        /// </summary>
        /// <param name="propertyInfo">The property to find the hint for</param>
        /// <returns>The <see cref="PropertyHint"/> for the property or NULL if the propery has no hint.</returns>
        public PropertyHint GetPropertyHint(PropertyInfo propertyInfo)
        {
            return Mappings.GetPropertyHint(propertyInfo);
        }

        /// <summary>
        /// Returns the RDF schema type for the specified entity interface or implementation type
        /// </summary>
        /// <param name="type">The entity interface or implementation type</param>
        /// <returns>The schema type URI for the entity type</returns>
        /// <exception cref="MappingNotFoundException">Raised if no mapping is found for <paramref name="type"/></exception>
        public string MapTypeToUri(Type type)
        {
            if (Mappings.IsMappedImplementation(type)) return Mappings.GetMappedInterfaceTypeUri(type);
            if (Mappings.IsKnownInterface(type)) return Mappings.GetMappedInterfaceTypeUri(Mappings.GetImplType(type));
            throw new MappingNotFoundException(type);
        }

        /// <summary>
        /// Returns the entity implementation type for an entity interface type
        /// </summary>
        /// <param name="interfaceType">The entity interface type</param>
        /// <returns>The mapped entity implementation type, or <paramref name="interfaceType"/> if no mapping is found</returns>
        public Type GetImplType(Type interfaceType)
        {
            return Mappings.GetImplType(interfaceType);
        }

        /// <summary>
        /// Converts an ID property value to a full URI
        /// </summary>
        /// <param name="identifierProperty">The property that provided the ID value</param>
        /// <param name="id">The ID value</param>
        /// <returns>The URI generated from the ID property value</returns>
        public abstract string MapIdToUri(PropertyInfo identifierProperty, string id);

        /// <summary>
        /// Deletes an entity object and all of its properties from the entity context
        /// </summary>
        /// <param name="o"></param>
        public abstract void DeleteObject(object o);

        /// <summary>
        /// Returns true if <paramref name="o"/> is an instance of one of the entity implementation types known to this context
        /// </summary>
        /// <param name="o">The object to be checked</param>
        /// <returns>True if the object is an instance of a known entity implementation type, false otherwise</returns>
        public bool IsOfMappedType(object o)
        {
            return Mappings.IsKnownInterface(o.GetType()) || Mappings.IsMappedImplementation(o.GetType());
        }

        /// <summary>
        /// Returns the URI of the RDF resource that represents the entity in the store
        /// </summary>
        /// <param name="o">The entity whose resource address is to be returned</param>
        /// <returns>The entity resource address</returns>
        public string GetResourceAddress(object o)
        {
            PropertyInfo identityProperty = Mappings.GetIdentityProperty(o.GetType());
            if (identityProperty != null)
            {
                var identityValue = identityProperty.GetValue(o, new object[0]) as string;
                if (identityValue != null)
                {
                    return MapIdToUri(identityProperty, identityValue);
                }
            }
            return
                o.GetType().GetProperties().Where(
                    p => p.GetCustomAttributes(typeof (IdentifierAttribute), true).Any()).Select(
                        p => p.GetValue(o, null).ToString()).FirstOrDefault();
        }


        /// <summary>
        /// Return the RDF datatype to apply to literals of the specified system type.
        /// </summary>
        /// <param name="systemType">The system type to be mapped</param>
        /// <returns>The RDF datatype URI for the specified system type</returns>
        /// <exception cref="ArgumentException">Raised of <paramref name="systemType"/> is not mapped to any RDF datatype known to this entity context</exception>
        public abstract string GetDatatype(Type systemType);

        #region Partial implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool disposing)
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
        /// This method is invoked when the entity context is being disposed.
        /// </summary>
        protected abstract void Cleanup();

        /// <summary>
        /// Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
        /// </summary>
        ~EntityContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
