using System.Collections.Generic;
using System.IO;
using System.Threading;
using BrightstarDB.Model;
using BrightstarDB.Rdf;

namespace BrightstarDB.Client
{
    internal class BrightstarRestUpdatableStore : IUpdateableStore
    {
        private readonly IBrightstarService _client;
        private readonly string _storeName;

        public BrightstarRestUpdatableStore(IBrightstarService client, string storeName)
        {
            _client = client;
            _storeName = storeName;
        }

        public Stream ExecuteQuery(string queryExpression, IList<string> datasetGraphUris)
        {
            return _client.ExecuteQuery(_storeName, queryExpression, datasetGraphUris);
        }

        public void ApplyTransaction(IList<Triple> preconditions, IList<Triple> deletePatterns, IList<Triple> inserts, 
            string updateGraphUri )
        {
            var deleteData = new StringWriter();
            var dw = new BrightstarTripleSinkAdapter(new NQuadsWriter(deleteData, updateGraphUri));
            foreach (Triple triple in deletePatterns)
            {
                dw.Triple(triple);
            }
            deleteData.Close();

            var addData = new StringWriter();
            var aw = new BrightstarTripleSinkAdapter(new NQuadsWriter(addData, updateGraphUri));
            foreach (Triple triple in inserts)
            {
                aw.Triple(triple);
            }
            addData.Close();

            var preconditionsData = new StringWriter();
            var pw = new BrightstarTripleSinkAdapter(new NQuadsWriter(preconditionsData, updateGraphUri));
            foreach (var triple in preconditions)
            {
                pw.Triple(triple);
            }
            preconditionsData.Close();

            PostTransaction(preconditionsData.ToString(), deleteData.ToString(), addData.ToString(), updateGraphUri);
        }

        public void Cleanup()
        {
            // Nothing to do
        }

        private void PostTransaction(string preconditions, string patternsToDelete, string triplesToAdd, string defaultGraphUri)
        {
            var jobInfo = _client.ExecuteTransaction(_storeName, preconditions, patternsToDelete, triplesToAdd, defaultGraphUri);

            while (!(jobInfo.JobCompletedOk || jobInfo.JobCompletedWithErrors))
            {
#if PORTABLE
    // Very rudimentary synchronous wait
                var ev = new ManualResetEvent(false);
                ev.WaitOne(200);
#else
                Thread.Sleep(20);
#endif
                jobInfo = _client.GetJobInfo(_storeName, jobInfo.JobId);
            }

            if (jobInfo.JobCompletedWithErrors)
            {
                // if (jobInfo.ExceptionInfo.Type == typeof(Server.PreconditionFailedException).FullName)
                if (jobInfo.ExceptionInfo != null && jobInfo.ExceptionInfo.Type == "BrightstarDB.Server.PreconditionFailedException")
                {
                    var triples = jobInfo.ExceptionInfo.Message.Substring(jobInfo.ExceptionInfo.Message.IndexOf('\n') + 1);
                    throw new TransactionPreconditionsFailedException(triples);
                }
                throw new BrightstarClientException("Error processing update transaction. " + jobInfo.StatusMessage);
            }
        }
    }
}