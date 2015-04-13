using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework.Query;
using BrightstarDB.EntityFramework.Tests.ContextObjects;

namespace BrightstarDB.EntityFramework.Tests
{
    /// <summary>
    /// A mock context that supports recording the last SPARQL query that was exectued.
    /// </summary>
    public class MockContext : EntityContext
    {
        private string _lastQuery;
        private SparqlLinqQueryContext _lastLinqQueryContext;
        public string LastSparqlQuery { get { return _lastQuery; } }
        public SparqlLinqQueryContext LastSparqlLinqQueryContext { get { return _lastLinqQueryContext; } }

        public MockContext() : base()
        {
            EntityMappingStore.Instance.SetImplMapping<IDinner, Dinner>();
            EntityMappingStore.Instance.SetImplMapping<ContextObjects.ICompany, ContextObjects.Company>();
            EntityMappingStore.Instance.SetImplMapping<ContextObjects.IMarket, ContextObjects.Market>();
            EntityMappingStore.Instance.SetImplMapping<ContextObjects.IPerson, ContextObjects.Person>();
            EntityMappingStore.Instance.SetImplMapping<IRsvp, Rsvp>();
            EntityMappingStore.Instance.SetImplMapping<IConcept, Concept>();
        }

        #region Overrides of LdoContext

        public override void SaveChanges()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates a single object in the object context with data from the data source
        /// </summary>
        /// <param name="mode">A <see cref="RefreshMode"/> value that indicates whether property changes
        /// in the object context are overwritten with property changes from the data source</param>
        /// <param name="entity">The object to be refreshed</param>
        public override void Refresh(RefreshMode mode, object entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update a collection of objects in the object context with data from the data source
        /// </summary>
        /// <param name="mode">A <see cref="RefreshMode"/> value that indicates whether property changes
        /// in the object context are overwritten with property changes from the data source</param>
        /// <param name="entities">The objects to be refreshed</param>
        public override void Refresh(RefreshMode mode, IEnumerable entities)
        {
            throw new NotImplementedException();
        }

        public override XDocument ExecuteQuery(string sparqlQuery)
        {
            _lastQuery = sparqlQuery;
            return new XDocument();
        }

        public override IEnumerable<T> ExecuteQuery<T>(SparqlLinqQueryContext sparqlLinqQuery)
        {
            _lastQuery = sparqlLinqQuery.SparqlQuery;
            _lastLinqQueryContext = sparqlLinqQuery;
            yield break;
        }

        public override IEnumerable<T> ExecuteInstanceQuery<T>(string instanceIdentifier, string typeIdentifier)
        {
            _lastQuery = String.Format("ASK {{ <{0}> a <{1}>. }}", instanceIdentifier, typeIdentifier);
            _lastLinqQueryContext = null;
            yield break;
        }

        public override string MapIdToUri(PropertyInfo propertyInfo, string id)
        {
            return ("id:" + Uri.EscapeUriString(id));
        }

        public override void DeleteObject(object o)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the RDF datatype to apply to literals of the specified system type.
        /// </summary>
        /// <param name="systemType"></param>
        /// <returns></returns>
        public override string GetDatatype(Type systemType)
        {
            if (typeof(Int32).Equals(systemType))
            {
                return Integer;
            }
            if (typeof(Decimal).Equals(systemType))
            {
                return Decimal;
            }
            if (typeof(Double).Equals(systemType))
            {
                return Double;
            }
            return null;
        }

        public override IList<string> GetDataset()
        {
            return null;
        }

        protected override void Cleanup()
        {
            // Nothing to clean up
        }

        #endregion

        public IQueryable<IDinner> Dinners { get { return new MockLdoSet<IDinner>(this); } }
        public IQueryable<IRsvp> Rsvps { get { return new MockLdoSet<IRsvp>(this); } }
        public IQueryable<ContextObjects.IMarket> Markets { get { return new MockLdoSet<ContextObjects.IMarket>(this); } }
        public IQueryable<ContextObjects.ICompany> Companies { get { return new MockLdoSet<ContextObjects.ICompany>(this); } }
        public IQueryable<ContextObjects.IPerson> People { get { return new MockLdoSet<ContextObjects.IPerson>(this); } }
        public IQueryable<IConcept> Concepts { get { return new MockLdoSet<IConcept>(this); } }

        /// <summary>
        /// The XML namespace for W3C XML Schema
        /// </summary>
        public const string XsdNamespace = "http://www.w3.org/2001/XMLSchema#";
        /// <summary>
        /// The XML Schema integer datatype URI
        /// </summary>
        public const string Integer = XsdNamespace + "integer";
        /// <summary>
        /// The XML Schema decimal datatype URI
        /// </summary>
        public const string Decimal = XsdNamespace + "decimal";

        public const string Double = XsdNamespace + "double";

    }
}
