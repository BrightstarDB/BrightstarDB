using System;
using System.Collections.Generic;
using System.IO;
using BrightstarDB.Dto;
using BrightstarDB.Rdf;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Exception raised when a transaction does not complete due to one or more precondition
    /// triples not matching.
    /// </summary>
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

        internal TransactionPreconditionsFailedException(string existenceFailures, string nonexistenceFailures)
            : base("Transaction preconditions were not met.")
        {
            FailedPreconditions = existenceFailures;
            if (existenceFailures != null)
            {
                try
                {
                    _invalidSubjects = new List<string>();
                    var p = new NTriplesParser();
                    using (var rdr = new StringReader(existenceFailures))
                    {
                        p.Parse(rdr, this, Constants.DefaultGraphUri);
                    }
                }
                catch
                {
                    // Ignore any errors when trying to parse the failed preconditions
                }
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

        /// <summary>
        /// Returns a new instance of the <see cref="TransactionPreconditionsFailedException"/> by retrieving
        /// the failed precondition strings from the provided <see cref="ExceptionDetailObject"/> instance. 
        /// </summary>
        /// <remarks>This method is used to "deserialize" a precondition failure exception from the ExceptionInfo
        /// field of a <see cref="IJobInfo"/> instance.</remarks>
        /// <param name="exceptionDetail"></param>
        /// <returns>A new <see cref="TransactionPreconditionsFailedException"/>.</returns>
        internal static Exception FromExceptionDetail(ExceptionDetailObject exceptionDetail)
        {
            string existenceFailures;
            string nonexistenceFailures;
            exceptionDetail.Data.TryGetValue("existenceFailedTriples", out existenceFailures);
            exceptionDetail.Data.TryGetValue("nonexistenceFailedTriples", out nonexistenceFailures);
            return new TransactionPreconditionsFailedException(existenceFailures, nonexistenceFailures);
        }
    }
}
