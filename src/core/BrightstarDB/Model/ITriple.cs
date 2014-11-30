namespace BrightstarDB.Model
{
    /// <summary>
    /// A very simple model for RDF triples (+context). 
    /// </summary>
    public interface ITriple
    {
        /// <summary>
        /// Get or set the Graph (context) URI for the triple.
        /// </summary>
        string Graph { get; set; }

        /// <summary>
        /// Get or set the subject resource URI
        /// </summary>
        string Subject { get; set; }

        /// <summary>
        /// Get or set the predicate resource URI
        /// </summary>
        string Predicate { get; set; }

        /// <summary>
        /// Get or set the object resource URI or the serialized
        /// string form of the object literal value.
        /// </summary>
        string Object { get; set; }

        /// <summary>
        /// Get or set the flag that indicates if the value of the <see cref="Object"/>
        /// property is a resource URI or a literal value.
        /// </summary>
        bool IsLiteral { get; set; }

        /// <summary>
        /// Get or set the data type URI
        /// </summary>
        string DataType { get; set; }

        /// <summary>
        /// Get or set the language code component for string literal values.
        /// </summary>
        string LangCode { get; set; }

        /// <summary>
        /// Returns true if this triple matches the specified triple allowing
        /// NULL in Graph, Subject, Predicate an Object to stand for a wildcard
        /// </summary>
        /// <param name="other">The other triple to match with</param>
        /// <returns>True if there is a match in the non-null parts of both triples, false otherwise</returns>
        bool Matches(ITriple other);
    }
}