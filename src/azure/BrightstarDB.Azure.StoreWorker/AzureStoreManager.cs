using System.IO;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore;

namespace BrightstarDB.Azure.StoreWorker
{
    internal class AzureStoreManager : BPlusTreeStoreManager
    {
        public AzureStoreManager(StoreConfiguration storeConfiguration) : base(storeConfiguration, new AzurePersistenceManager())
        {
            
        }
        public AzureStoreManager() : base(StoreConfiguration.DefaultStoreConfiguration, new AzurePersistenceManager())
        {
            
        }

        public override ITransactionLog GetTransactionLog(string storeLocation)
        {
            return new DummyTransactionLog();
        }
    }

    internal class DummyTransactionLog : ITransactionLog
    {
        #region Implementation of ITransactionLog

        public void LogStartTransaction(ILoggable itemToLog)
        {
            return;
        }

        public void LogEndSuccessfulTransaction(ILoggable itemToLog)
        {
            return;
        }

        public void LogEndFailedTransaction(ILoggable itemToLog)
        {
            return;
        }

        public ITransactionInfo ReadTransactionInfo(int transactionNumber)
        {
            return null;
        }

        public Stream GetTransactionData(ulong dataStartPosition)
        {
            return null;
        }

        public TransactionInfoEnumerator GetTransactionList()
        {
            return new TransactionInfoEnumerator(null);
        }

        #endregion
    }
}
