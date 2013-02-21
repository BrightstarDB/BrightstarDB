using System;
using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Rdf;

namespace BrightstarDB.Azure.StoreWorker
{
    internal class AzureImportJob : ITripleSink
    {
        private readonly string _jobId;
        private readonly BlobImportSource _importSource;
        private readonly Server.StoreWorker _worker;
        private ITripleSink _importTripleSink;
        private readonly string _graphUri;
        private long _tripleCount;
        private Stream _importStream;
        private readonly Action<string, string> _statusCallback;
        public bool Errors { get; private set; }
        public string ErrorMessage { get; private set; }

        public AzureImportJob(string jobId, Server.StoreWorker worker, BlobImportSource importSource, Action<string, string> statusCallback)
        {
            _jobId = jobId;
            _worker = worker;
            _importSource = importSource;
            _graphUri = Constants.DefaultGraphUri;
            _statusCallback = statusCallback;
        }

        public void Run()
        {
            try
            {
                var jobGuid = Guid.Parse(_jobId);
                using (_importStream = _importSource.OpenRead())
                {
                    var parser = new NTriplesParser();
                    _importTripleSink = new StoreTripleSink(_worker.WriteStore, jobGuid,
                                                            Configuration.TransactionFlushTripleCount);
                    parser.Parse(_importStream, this, _graphUri);
                    _importStream.Close();
                }
                _worker.WriteStore.Commit(jobGuid);
                _worker.InvalidateReadStore();
            }
            catch (RdfParserException parserException)
            {
                Logging.LogError(
                    BrightstarEventId.ImportDataError,
                    "Encountered parser error : {0}", parserException);
                _statusCallback(_jobId,
                                String.Format("Import failed due to parser error: {0}", parserException));
                Errors = true;
                ErrorMessage = parserException.HaveLineNumber
                                   ? String.Format("Parser error at line {0}: {1}", parserException.LineNumber,
                                                   parserException.Message)
                                   : String.Format("Parser error: {0}", parserException.Message);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.JobProcessingError,
                                 "Error processing import job on source " + _importSource + ". Error Message: " +
                                 ex.Message + " Stack trace: " + ex.StackTrace);
                throw;
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
            try
            {
                _importTripleSink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, obj, objIsBNode,
                                         objIsLiteral,
                                         dataType, langCode, graphUri);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ImportDataError, "Error importing triple. Cause: {0}", ex);
                throw;
            }

            _tripleCount++;
            if (_tripleCount%1000 == 0 && _statusCallback != null)
            {
                Logging.LogInfo("Job {0} imported {1} triples.", _jobId, _tripleCount);
                string statusMessage;
                try
                {
                    double percentComplete = ((double) _importStream.Position/(_importStream.Length));
                    statusMessage = String.Format("Imported {0:N0} triples. Approximately {1:P1} complete",
                                                  _tripleCount, percentComplete);
                }
                catch (Exception)
                {
                    // This may happen if the stream length is not known
                    statusMessage = String.Format("Imported {0:N0} triples.", _tripleCount);
                }

                _statusCallback(_jobId, statusMessage);
            }
        }

        #endregion
    }
}