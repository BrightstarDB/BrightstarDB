using System.Collections;
using System.Collections.Generic;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Used to iterate over the transactions in a persistent transaction log
    /// </summary>
    internal class TransactionInfoEnumerator : IEnumerator<ITransactionInfo>
    {
        /// <summary>
        /// This is the position in the log for this particular list
        /// </summary>
        private int _transactionNumber;
        private readonly ITransactionLog _transactionLog;
        private ITransactionInfo _current;

        internal TransactionInfoEnumerator(ITransactionLog log)
        {
            _transactionLog = log;
            _transactionNumber = 0;
        }

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            _transactionNumber++;
            _current = _transactionLog != null ? _transactionLog.ReadTransactionInfo(_transactionNumber) : null;
            return _current != null;
        }

        public void Reset()
        {
            _transactionNumber = 0;
        }

        public ITransactionInfo Current
        {
            get { return _current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}
