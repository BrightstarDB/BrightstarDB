using System;
using System.IO;
using System.Linq;
using BrightstarDB.Profiling;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Parsing.Tokens;

#if !SILVERLIGHT
using System.ServiceModel;
#endif

#if PORTABLE
using BrightstarDB.Portable.Adaptation;
using Path = BrightstarDB.Portable.Compatibility.Path;
#endif

namespace BrightstarDB.Server
{
    internal class ImportJob : UpdateJob, ITripleSink
    {
        private string _contentFileName;
        private string _graphUri = Constants.DefaultGraphUri;
        private ITripleSink _importTripleSink;
        private Stream _fileStream;
        private int _tripleCount;

        public ImportJob(Guid jobId, StoreWorker storeWorker, string contentFileName, string graphUri)
            : base(jobId, storeWorker)
        {
            _contentFileName = contentFileName;
            _graphUri = graphUri;
        }

        public ImportJob(Guid jobId, StoreWorker storeWorker) : base(jobId, storeWorker)
        {
        }

        public override void Run()
        {
            try
            {
                Logging.LogInfo("Import job being run on file " + _contentFileName);
                StoreWorker.TransactionLog.LogStartTransaction(this);

                var parser = GetParser(_contentFileName);
                var storeDirectory = StoreWorker.WriteStore.DirectoryPath;
                var importDirectory = Path.Combine(Path.GetDirectoryName(storeDirectory), "import");
                var filePath = Path.Combine(importDirectory, _contentFileName);
                var profiler = Logging.IsProfilingEnabled ? new BrightstarProfiler("Import " + _contentFileName) : null;
                Logging.LogDebug("Import file path calculated as '{0}'", filePath);

                using (_fileStream = GetImportFileStream(filePath))
                {
                    _importTripleSink = new StoreTripleSink(StoreWorker.WriteStore, JobId,
                                                            Configuration.TransactionFlushTripleCount,
                                                            profiler:profiler);
                    parser.Parse(_fileStream, this, _graphUri);
                }
                StoreWorker.WriteStore.Commit(JobId, profiler);
                StoreWorker.InvalidateReadStore();

                Logging.LogInfo("Import job completed successfully for " + _contentFileName);
                if (profiler != null)
                {
                    Logging.LogInfo(profiler.GetLogString());
                }
                StoreWorker.TransactionLog.LogEndSuccessfulTransaction(this);
            }
            catch (RdfParserException parserException)
            {
                ErrorMessage = parserException.Message;
                ExceptionDetail = new ExceptionDetail(parserException);
                Logging.LogInfo("Parser error processing import job on file " + _contentFileName + ". " + parserException.Message);
                throw;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error importing file " + _contentFileName + ". " + ex.Message;
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogInfo("Error processing import job on file " + _contentFileName + ". Error Message: " + ex.Message + " Stack trace: " + ex.StackTrace);
                throw;
            }
        }

        private Stream GetImportFileStream(string filePath)
        {
#if PORTABLE
            var persistenceManager = PlatformAdapter.Resolve<IPersistenceManager>();
            if (!persistenceManager.FileExists(filePath))
            {
                ErrorMessage = String.Format("Cannot find file {0} in import directory", _contentFileName);
                throw new FileNotFoundException(ErrorMessage);                
            }
            return persistenceManager.GetInputStream(filePath);
#else
            if (!File.Exists(filePath))
                {
                    ErrorMessage = String.Format("Cannot find file {0} in import directory", _contentFileName);
                    throw new FileNotFoundException(ErrorMessage);
                }
                return _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
#endif
        }

        #region implementation of ILoggable

        /// <summary>
        /// Logs the name of the file imported. 
        /// </summary>
        /// <param name="logStream"></param>
        public override void LogTransactionDataToStream(Stream logStream)
        {
            using (var writer = new BinaryWriter(logStream))
            {
                writer.Write(_contentFileName);
                writer.Write(_graphUri);
            }
        }

        public override void ReadTransactionDataFromStream(Stream logStream)
        {
            using (var reader = new BinaryReader(logStream))
            {
                _contentFileName = reader.ReadString();
                _graphUri =  reader.ReadString();
            }
        }

        public override TransactionType TransactionType { get { return TransactionType.ImportJob; } }
        #endregion

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
            _importTripleSink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, obj, objIsBNode, objIsLiteral, dataType, langCode, graphUri);
            _tripleCount++;
            if (_tripleCount % 1000 == 0)
            {
                var percentComplete = ((double)_fileStream.Position / (_fileStream.Length));
                var jobStatus = StoreWorker.GetJobStatus(JobId.ToString());
                if (jobStatus != null)
                {
                    jobStatus.Information = String.Format("Imported {0:N0} triples. Approximately {1:P1} complete",
                                                          _tripleCount, percentComplete);
                }
            }
        }

        #endregion

        private static IRdfParser GetParser(string fileName)
        {
            Options.DefaultTokenQueueMode = TokenQueueMode.SynchronousBufferDuringParsing;
            var fileExtension = MimeTypesHelper.GetTrueFileExtension(fileName);
#if PORTABLE
            bool isGZiped =
                fileExtension.ToLowerInvariant().EndsWith(MimeTypesHelper.DefaultGZipExtension.ToLowerInvariant());
#else
            bool isGZiped = fileExtension.EndsWith(MimeTypesHelper.DefaultGZipExtension, StringComparison.InvariantCultureIgnoreCase);
#endif
            var parserDefinition = MimeTypesHelper.GetDefinitionsByFileExtension(fileExtension).FirstOrDefault(
                def => def.CanParseRdf);
            if (parserDefinition != null)
            {
                var rdfReader = parserDefinition.GetRdfParser();
                if (rdfReader != null)
                {
                    if (rdfReader is VDS.RDF.Parsing.NTriplesParser && !isGZiped)
                    {
                        // Use the Brighstar NTriples Parser
                        return new NTriplesParser();
                    }
                    return new BrightstarRdfParserAdapter(rdfReader, isGZiped);
                }
            }
            Logging.LogWarning(BrightstarEventId.ParserWarning,
                               "Unable to select a parser by determining MIME type from file extension. Will default to NTriples parser.");
            return new NTriplesParser();
        }
    }
}
