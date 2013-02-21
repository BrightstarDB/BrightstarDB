namespace BrightstarDB.Rdf
{
    /// <summary>
    /// The interface implemented by any class that receives a sequence of RDF statements from a triple
    /// generator such as an RDF parser or a data model serializer.
    /// </summary>
    public interface ITripleSink
    {
        /// <summary>
        /// Handler method for an individual RDF statement
        /// </summary>
        /// <param name="subject">The statement subject resource URI</param>
        /// <param name="subjectIsBNode">True if the value of <paramref name="subject"/> is a BNode identifier</param>
        /// <param name="predicate">The predicate resource URI</param>
        /// <param name="predicateIsBNode">True if the value of <paramref name="predicate"/> is a BNode identifier.</param>
        /// <param name="obj">The object of the statement</param>
        /// <param name="objIsBNode">True if the value of <paramref name="obj"/> is a BNode identifier.</param>
        /// <param name="objIsLiteral">True if the value of <paramref name="obj"/> is a literal string</param>
        /// <param name="dataType">The datatype URI for the object literal or null if the object is not a literal</param>
        /// <param name="langCode">The language code for the object literal or null if the object is not a literal</param>
        /// <param name="graphUri">The graph URI for the statement</param>
        void Triple(string subject, bool subjectIsBNode,
            string predicate, bool predicateIsBNode, 
            string obj, bool objIsBNode, bool objIsLiteral, 
            string dataType, string langCode, string graphUri);
    }
}
