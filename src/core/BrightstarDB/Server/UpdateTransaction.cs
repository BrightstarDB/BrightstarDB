using System;
using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Rdf;

namespace BrightstarDB.Server
{
    internal class UpdateTransaction : UpdateJob
    {
        // these triples must exist in order for the rest of the operation to continue
        private string _preconditions;
        // triples patterns that match triples to be deleted
        private string _deletePatterns;
        // triples to add
        private string _insertData;

        public UpdateTransaction(Guid jobId, StoreWorker storeWorker, string preconditionData, string deletePatterns, string insertData) : base(jobId, storeWorker)
        {
            _deletePatterns = deletePatterns ?? "";
            _insertData = insertData ?? "";
            _preconditions = preconditionData ?? "";
        }

        public UpdateTransaction(Guid jobId, StoreWorker storeWorker) : base(jobId, storeWorker) {}

        public string DeletePatterns
        {
            get { return _deletePatterns; }
        }

        public string InsertData
        {
            get { return _insertData; }
        }

        public string Preconditions
        {
            get { return _preconditions; }
        }

        public override void Run()
        {
            try
            {
                StoreWorker.TransactionLog.LogStartTransaction(this);

                var writeStore = StoreWorker.WriteStore;

                // process preconditions
                Logging.LogInfo("UpdateTransaction {0} - processing preconditions", JobId);
                try
                {
                    var preconditionSink = new PreconditionSink(writeStore);
                    var parser = new NTriplesParser();
                    parser.Parse(new StringReader(_preconditions), preconditionSink, Constants.DefaultGraphUri);
                    if (preconditionSink.FailedPreconditionCount > 0)
                    {
                        throw new PreconditionFailedException(preconditionSink.FailedPreconditionCount, preconditionSink.GetFailedPreconditions());
                    }
                }
                catch (RdfParserException parserException)
                {
                    throw new BrightstarClientException("Syntax error in preconditions.", parserException);
                }

                // process deletes 
                Logging.LogInfo("UpdateTransaction {0} - processing deletes", JobId);
                try
                {
                    var delSink = new DeletePatternSink(writeStore);
                    var parser = new NTriplesParser();
                    parser.Parse(new StringReader(_deletePatterns), delSink, Constants.DefaultGraphUri);
                }
                catch (RdfParserException parserException)
                {
                    throw new BrightstarClientException("Syntax error in delete patterns.", parserException);
                }

                try
                {
                    // insert data
                    Logging.LogInfo("UpdateTransaction {0} - processing inserts", JobId);
                    var parser = new NTriplesParser();
                    parser.Parse(new StringReader(_insertData),
                                 new StoreTripleSink(writeStore, JobId, Configuration.TransactionFlushTripleCount),
                                 Constants.DefaultGraphUri);
                }
                catch (RdfParserException parserException)
                {
                    throw new BrightstarClientException("Syntax error in triples to add.", parserException);
                }

                // commit changes
                Logging.LogInfo("UpdateTransaction {0} - committing changes", JobId);
                writeStore.Commit(JobId);

                // change read store
                Logging.LogInfo("UpdateTransaction {0} - invalidating read store", JobId);
                StoreWorker.InvalidateReadStore();

                // log txn completed 
                Logging.LogInfo("UpdateTransaction {0} - logging completion", JobId);
                StoreWorker.TransactionLog.LogEndSuccessfulTransaction(this);
                Logging.LogInfo("UpdateTransaction {0} - done", JobId);
            }
            catch (PreconditionFailedException ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogInfo("Preconditions failed in UpdateTransaction ({0}): Count={1}, Triples={2}", JobId, ex.FailureCount, ex.FailedTriples);
                throw;
            }
            catch (BrightstarClientException ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogError(BrightstarEventId.TransactionClientError,
                                 "Client error reported in UpdateTransaction ({0}): {1}", JobId, ex.InnerException.ToString());
                throw;
            }
            catch (Exception ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogError(BrightstarEventId.TransactionServerError,
                                 "Unexpected exception caught in UpdateTransaction ({0}): {1}",JobId, ex);
                throw;
            }
        }

        public override void LogTransactionDataToStream(Stream logStream)
        {
            using (var writer = new BinaryWriter(logStream))
            {
                writer.Write(_preconditions);
                writer.Write(_deletePatterns);
                writer.Write(_insertData);
            }
        }

        public override void ReadTransactionDataFromStream(Stream logStream)
        {
            using (var reader = new BinaryReader(logStream))
            {
                _preconditions = reader.ReadString();
                _deletePatterns = reader.ReadString();
                _insertData = reader.ReadString();
            }
        }

        public override Storage.TransactionType TransactionType { get { return Storage.TransactionType.UpdateTransaction; } }
    }
}
