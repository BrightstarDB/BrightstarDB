using System;
using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Rdf;

namespace BrightstarDB.Server
{
    internal class GuardedUpdateTransaction : UpdateJob
    {
        /// <summary>
        /// The default graph to apply the transaction to
        /// </summary>
        private string _defaultGraphUri;

        public GuardedUpdateTransaction(Guid jobId, string label, StoreWorker storeWorker, string existsPreconditions, string notExistsPreconditions, string deletePatterns, string insertData, string defaultGraphUri) : 
            base(jobId, label, storeWorker)
        {
            _defaultGraphUri = defaultGraphUri ?? Constants.DefaultGraphUri;
            DeletePatterns = deletePatterns ?? String.Empty;
            InsertData = insertData ?? String.Empty;
            ExistsPreconditions = existsPreconditions ?? String.Empty;
            NotExistsPreconditions = notExistsPreconditions ?? String.Empty;
        }

        public GuardedUpdateTransaction(Guid jobId, string label, StoreWorker storeWorker) : base(jobId, label, storeWorker) {}

        public string DeletePatterns { get; private set; }

        public string InsertData { get; private set; }

        public string ExistsPreconditions { get; private set; }

        public string NotExistsPreconditions { get; private set; }

        public override void Run()
        {
            try
            {
                StoreWorker.TransactionLog.LogStartTransaction(this);

                var writeStore = StoreWorker.WriteStore;

                // process preconditions
                Logging.LogInfo("GuardedUpdateTransaction {0} - processing preconditions", JobId);
                try
                {
                    var existsSink = new PreconditionSink(writeStore, PreconditionSink.PreconditionType.ExistsPrecondition);
                    var parser = new NTriplesParser();
                    parser.Parse(new StringReader(ExistsPreconditions), existsSink, _defaultGraphUri);
                    var notExistsSink = new PreconditionSink(writeStore,
                                                             PreconditionSink.PreconditionType.NotExistsPrecondition);
                    parser.Parse(new StringReader(NotExistsPreconditions), notExistsSink, _defaultGraphUri );
                    if (existsSink.FailedPreconditionCount > 0 || notExistsSink.FailedPreconditionCount > 0)
                    {
                        throw new PreconditionFailedException(existsSink.FailedPreconditionCount,
                                                              existsSink.GetFailedPreconditions(),
                                                              notExistsSink.FailedPreconditionCount,
                                                              notExistsSink.GetFailedPreconditions());
                    }
                }
                catch (RdfParserException parserException)
                {
                    throw new BrightstarClientException("Syntax error in preconditions.", parserException);
                }

                // process deletes 
                Logging.LogInfo("GuardedUpdateTransaction {0} - processing deletes", JobId);
                try
                {
                    var delSink = new DeletePatternSink(writeStore);
                    var parser = new NTriplesParser();
                    parser.Parse(new StringReader(DeletePatterns), delSink, _defaultGraphUri);
                }
                catch (RdfParserException parserException)
                {
                    throw new BrightstarClientException("Syntax error in delete patterns.", parserException);
                }

                try
                {
                    // insert data
                    Logging.LogInfo("GuardedUpdateTransaction {0} - processing inserts", JobId);
                    var parser = new NTriplesParser();
                    parser.Parse(new StringReader(InsertData),
                                 new StoreTripleSink(writeStore, JobId, Configuration.TransactionFlushTripleCount),
                                 _defaultGraphUri);
                }
                catch (RdfParserException parserException)
                {
                    throw new BrightstarClientException("Syntax error in triples to add.", parserException);
                }

                // commit changes
                Logging.LogInfo("GuardedUpdateTransaction {0} - committing changes", JobId);
                writeStore.Commit(JobId);

                // change read store
                Logging.LogInfo("GuardedUpdateTransaction {0} - invalidating read store", JobId);
                StoreWorker.InvalidateReadStore();

                // log txn completed 
                Logging.LogInfo("GuardedUpdateTransaction {0} - logging completion", JobId);
                StoreWorker.TransactionLog.LogEndSuccessfulTransaction(this);
                Logging.LogInfo("GuardedUpdateTransaction {0} - done", JobId);
            }
            catch (PreconditionFailedException ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogInfo(
                    "Preconditions failed in GuardedUpdateTransaction ({0}): Count={1}.\nMissing Triples:\n{2}\nUnexpectedTriples:\n{3}",
                    JobId,
                    ex.ExistanceFailureCount + ex.NonExistanceFailureCount, ex.ExistanceFailedTriples,
                    ex.NonExistanceFailedTriples);
                throw;
            }
            catch (BrightstarClientException ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogError(BrightstarEventId.TransactionClientError,
                                 "Client error reported in GuardedUpdateTransaction ({0}): {1}", JobId, ex.InnerException.ToString());
                throw;
            }
            catch (Exception ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogError(BrightstarEventId.TransactionServerError,
                                 "Unexpected exception caught in GuardedUpdateTransaction ({0}): {1}",JobId, ex);
                throw;
            }
        }

        public override void LogTransactionDataToStream(Stream logStream)
        {
            using (var writer = new BinaryWriter(logStream))
            {
                writer.Write(ExistsPreconditions);
                writer.Write(NotExistsPreconditions);
                writer.Write(DeletePatterns);
                writer.Write(InsertData);
                writer.Write(_defaultGraphUri);
            }
        }

        public override void ReadTransactionDataFromStream(Stream logStream)
        {
            using (var reader = new BinaryReader(logStream))
            {
                ExistsPreconditions = reader.ReadString();
                NotExistsPreconditions = reader.ReadString();
                DeletePatterns = reader.ReadString();
                InsertData = reader.ReadString();
                _defaultGraphUri = reader.ReadString();
            }
        }

        public override TransactionType TransactionType { get { return TransactionType.GuardedUpdateTransaction; } }
    }
}
