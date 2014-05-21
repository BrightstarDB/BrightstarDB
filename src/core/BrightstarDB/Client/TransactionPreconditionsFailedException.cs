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
#if WINDOWS_PHONE
        private readonly Dictionary<string, bool> _invalidSubjects;
        private readonly Dictionary<string, bool> _invalidNonExistenceSubjects;
#else
        private readonly HashSet<string> _invalidSubjects;
        private readonly HashSet<string> _invalidNonExistenceSubjects;
#endif

        /// <summary>
        /// Returns the failed precondition triples in NTriples format
        /// </summary>
        public string FailedPreconditions { get; private set; }

        /// <summary>
        /// Returns the triples that failed the non-existence precondition tests
        /// </summary>
        public string FailedNonExistencePreconditions { get; private set; }

#if WINDOWS_PHONE
        /// <summary>
        /// Returns an enumeration over the subject resource URIs for all preconditions reported
        /// as not being met.
        /// </summary>
        public IEnumerable<string> InvalidSubjects { get { return _invalidSubjects.Keys; } }

        /// <summary>
        /// Returns an enumeration over the subject resource URIs for all triples that failed
        /// the non-existence precondition.
        /// </summary>
        public IEnumerable<string> InvalidNonExistenceSubjects { get { return _invalidNonExistenceSubjects.Keys; } }
#else
        /// <summary>
        /// Returns an enumeration over the subject resource URIs for all preconditions reported
        /// as not being met.
        /// </summary>
        public IEnumerable<string> InvalidSubjects { get { return _invalidSubjects; } }

        /// <summary>
        /// Returns an enumeration over the subject resource URIs for all triples that failed
        /// the non-existence precondition.
        /// </summary>
        public IEnumerable<string> InvalidNonExistenceSubjects { get { return _invalidNonExistenceSubjects; } }
#endif
        private bool _parsingNonexistenceFailures;


        internal TransactionPreconditionsFailedException(string existenceFailures, string nonexistenceFailures)
            : base("Transaction preconditions were not met.")
        {
            FailedPreconditions = existenceFailures;
            if (existenceFailures != null)
            {
                try
                {
#if WINDOWS_PHONE
                    _invalidSubjects = new Dictionary<string, bool>();
#else
                    _invalidSubjects = new HashSet<string>();
#endif
                    var p = new NTriplesParser();
                    this._parsingNonexistenceFailures = false;
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

            FailedNonExistencePreconditions = nonexistenceFailures;
            if (nonexistenceFailures != null)
            {
                try
                {
#if WINDOWS_PHONE
                    _invalidNonExistenceSubjects = new Dictionary<string, bool>();
#else
                    _invalidNonExistenceSubjects = new HashSet<string>();
#endif
                    this._parsingNonexistenceFailures = true;
                    var p = new NTriplesParser();
                    using (var rdr = new StringReader(nonexistenceFailures))
                    {
                        p.Parse(rdr, this, Constants.DefaultGraphUri);
                    }
                }
                catch
                {
                    // Ignore errors when trying to parse the failed preconditions
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
#if WINDOWS_PHONE
            if (_parsingNonexistenceFailures)
            {
                _invalidNonExistenceSubjects[subject] = true;
            }
            else
            {
                _invalidSubjects[subject] = true;
            }
#else
            if (_parsingNonexistenceFailures)
            {
                _invalidNonExistenceSubjects.Add(subject);
            }
            else
            {
                _invalidSubjects.Add(subject);
            }
#endif
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
