using System;
using System.IO;
using BrightstarDB.Storage;
using BrightstarDB.Update;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace BrightstarDB.Server
{
    internal class SparqlUpdateJob : UpdateJob
    {
        private string _updateExpression;
        private readonly SparqlUpdateParser _parser = new SparqlUpdateParser();

        public SparqlUpdateJob(Guid jobId, StoreWorker storeWorker, string updateExpression) : base(jobId, storeWorker)
        {
            _updateExpression = updateExpression;               
        }

        public string Expression
        {
            get { return _updateExpression; }
        }

        #region Overrides of Job

        public override void Run()
        {
            try
            {
                Logging.LogInfo("SPARQL update job being run on expression '{0}'", _updateExpression);
                StoreWorker.TransactionLog.LogStartTransaction(this);

                var processor = new BrightstarUpdateProcessor(new BrightstarIOManager(StoreWorker.WriteStore));
                var cmds = _parser.ParseFromString(_updateExpression);
                processor.ProcessCommandSet(cmds);
                StoreWorker.WriteStore.Commit(JobId);
                StoreWorker.InvalidateReadStore();

                Logging.LogInfo("SPARQL update job completed successfully");
                StoreWorker.TransactionLog.LogEndSuccessfulTransaction(this);

            }
            catch (RdfException ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogInfo("Error processing SPARQL update expression '{0}'. Error Message: {1} Stack Trace: {2}",
                    _updateExpression, ex.Message, ex.StackTrace);
                ErrorMessage = String.Format("Error processing SPARQL update expression. {0}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                StoreWorker.TransactionLog.LogEndFailedTransaction(this);
                Logging.LogInfo("Error processing SPARQL update expression '{0}'. Error Message: {1} Stack Trace: {2}",
                                _updateExpression,
                                ex.Message, ex.StackTrace);
                throw;
            }

        }

        public override void LogTransactionDataToStream(Stream logStream)
        {
            using(var writer = new BinaryWriter(logStream))
            {
                writer.Write(_updateExpression);
            }
        }

        public override void ReadTransactionDataFromStream(Stream logStream)
        {
            using(var reader = new BinaryReader(logStream))
            {
                _updateExpression = reader.ReadString();
            }
        }

        public override TransactionType TransactionType { get { return TransactionType.SparqlUpdateTransaction; } }

        #endregion
    }
}
