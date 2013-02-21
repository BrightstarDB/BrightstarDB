using System.Collections.Generic;
using System.IO;
using BrightstarDB.Rdf;
using SmartAssembly.Attributes;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Exception raised when a transaction does not complete due to one or more precondition
    /// triples not matching.
    /// </summary>
    [DoNotObfuscate]
    public class TransactionPreconditionsFailedException : BrightstarException, ITripleSink
    {
        private readonly List<string> _invalidSubjects;

        /// <summary>
        /// Returns the failed precondition triples in NTriples format
        /// </summary>
        public string FailedPreconditions { get; private set; }

        /// <summary>
        /// Returns an enumeration over the subject resource URIs for all preconditions reported
        /// as not being met.
        /// </summary>
        public IEnumerable<string> InvalidSubjects { get { return _invalidSubjects; } }

        internal TransactionPreconditionsFailedException(string failedTriples)
            : base("Transaction preconditions were not met.")
        {
            FailedPreconditions = failedTriples;
            try
            {
                _invalidSubjects = new List<string>();
                var p = new NTriplesParser();
                using (var rdr = new StringReader(failedTriples))
                {
                    p.Parse(rdr, this, Constants.DefaultGraphUri);
                }
            }
            catch
            {
                // Ignore any errors when trying to parse the failed preconditions
            }
        }

        #region Implementation of ITripleSink

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
        public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool objIsLiteral, string dataType, string langCode, string graphUri)
        {
            _invalidSubjects.Add(subject);
        }

        #endregion
    }
}
