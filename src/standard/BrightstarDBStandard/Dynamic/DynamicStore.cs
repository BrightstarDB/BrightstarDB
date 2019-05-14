using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework.Query;

namespace BrightstarDB.Dynamic
{
    /// <summary>
    /// A store that exposes RDF data via .NET dynamic objects
    /// </summary>
    public class DynamicStore
    {
        private readonly IDataObjectStore _store;

        /// <summary>
        /// Initialises a new store with the specified underlying 
        /// </summary>
        /// <param name="store"></param>
        public DynamicStore(IDataObjectStore store)
        {
            _store = store;            
        }

        /// <summary>
        /// Create a new dynamic object
        /// </summary>
        /// <param name="prefix">optional identity prefix</param>
        /// <returns>A new dynamic object</returns>
        public dynamic MakeNewObject(string prefix = null)
        {
            var obj = _store.MakeNewDataObject();
            return new BrightstarDynamicObject(obj);
        }

        /// <summary>
        /// Gets a new dynamic data object whose data is populated with data from the indicated resource.
        /// </summary>
        /// <param name="identity">The identity of the resource </param>
        /// <returns>A new dynamic object</returns>
        public dynamic GetDataObject(string identity)
        {
            var obj = _store.GetDataObject(identity);
            return new BrightstarDynamicObject(obj);
        }

        /// <summary>
        /// Gets an enumeration of dynamic objects, where each is bound to the data attached to each result in the provided SPARQL query.
        /// </summary>
        /// <param name="sparqlExpression">The sparql query that indentifies the objects to return.</param>
        /// <returns>A collection of dynamic objects bound to the SPARQL result.</returns>
        public IEnumerable<dynamic> BindObjectsWithSparql(string sparqlExpression)
        {
            return _store.BindDataObjectsWithSparql(sparqlExpression).Select(dataObject => new BrightstarDynamicObject(dataObject)).Cast<dynamic>();
        }

        /// <summary>
        /// Execute a SPARQL query against the underlying store.
        /// </summary>
        /// <param name="sparqlExpression">The SPARQL query to execute</param>
        /// <returns>A SPARQL result</returns>
        public SparqlResult ExecuteSparql(string sparqlExpression)
        {
            return _store.ExecuteSparql(sparqlExpression);
        }

        /// <summary>
        /// Commits all changes
        /// </summary>
        public void SaveChanges()
        {
            _store.SaveChanges();
        }
    }
}
