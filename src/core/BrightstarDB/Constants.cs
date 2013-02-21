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
        /// A regular expression to use for validating store names
        /// </summary>
        public const string StoreNameRegex = @"^[a-zA-Z0-9-_\.\+,\(\)]{1,1024}$";

    }
}
