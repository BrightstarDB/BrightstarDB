using BrightstarDB.Rdf;

namespace BrightstarDB
{
    /// <summary>
    /// Provides access to a set of RDF-related constants used by BrighstarDB
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// A constant that defines the URI assigned to the default graph in a BrightstarDB store
        /// </summary>
        public const string DefaultGraphUri = "http://www.brightstardb.com/.well-known/model/defaultgraph";

        /// <summary>
        /// A constant that defines the prefix part of auto-generated URIs created by BrightstarDB
        /// </summary>
        public const string GeneratedUriPrefix = "http://www.brightstardb.com/.well-known/genid/";

        /// <summary>
        /// A constant that defines a wildcard resource match in a delete pattern
        /// </summary>
        public const string WildcardUri = "http://www.brightstardb.com/.well-known/model/wildcard";

        /// <summary>
        /// A constant that defines the default datatype assigned to literal values
        /// </summary>
        public const string DefaultDatatypeUri = RdfDatatypes.PlainLiteral;

        /// <summary>
        /// Predicate URI used in optimistic locking
        /// </summary>
        public const string VersionPredicateUri = "http://www.brightstardb.com/.well-known/model/version";

        /// <summary>
        /// Predicate used in eagerly loaded LINQ to SPARQL result graphs
        /// </summary>
        public const string SelectVariablePredicateUri = "http://www.brightstardb.com/.well-known/model/selectVariable";

        /// <summary>
        /// Base string for constrtucting sort value predicates in an eagerly loaded LINQ to SPARQL result graph.
        /// The actualy URIs have an integer appended, starting with 0 (http://www.brightstardb.com/.well-known/model/sortValue0)
        /// </summary>
        public const string SortValuePredicateBase = "http://www.brightstardb.com/.well-known/model/sortValue";

        /// <summary>
        /// A regular expression to use for validating store names
        /// </summary>
        public const string StoreNameRegex = @"^[a-zA-Z0-9-_\.\+,\(\)]{1,1024}$";

    }
}
