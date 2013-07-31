using System;
using System.Collections.Generic;
using System.IO;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Storage.TransactionLog
{
    internal class PersistentTransactionLog : ITransactionLog
    {
        private readonly IPersistenceManager _persistenceManager;
        private readonly string _storeLocation;

        internal PersistentTransactionLog(IPersistenceManager persistenceManager, string storeLocation)
        {
            _persistenceManager = persistenceManager;
            _storeLocation = storeLocation;
        }

        #region Implementation of ITransactionLog

        /// <summary>
        /// This value is set when the data is persisted. It is written to the transaction header file when 
        /// the transaction is completed.
        /// </summary>
        private ulong _currentTransactionDataStartPosition;

        /// <summary>
        /// Logs the transaction data to the file and remembers the start position.
        /// </summary>
        /// <param name="itemToLog"></param>
        public void LogStartTransaction(ILoggable itemToLog)
        {
            // get the start position
            _currentTransactionDataStartPosition = (ulong) _persistenceManager.GetFileLength(GetTransactionLogFile());

            // now write the transaction data
            using (var stream = _persistenceManager.GetOutputStream(GetTransactionLogFile(), FileMode.Append))
            {
                itemToLog.LogTransactionDataToStream(stream);                
            }
        }

        /// <summary>
        /// Called when a job completes successfully so that the header can be written.
        /// </summary>
        /// <param name="itemToLog"></param>
        public void LogEndSuccessfulTransaction(ILoggable itemToLog)
        {
            var endPosition = (ulong) _persistenceManager.GetFileLength(GetTransactionLogFile());
            var contentLength = endPosition - _currentTransactionDataStartPosition;

            using (var stream = _persistenceManager.GetOutputStream(GetTransactionLogHeaderFile(), FileMode.Append))
            {
                var transactionHeader = new TransactionInfo(itemToLog.TransactionId, TransactionStatus.CompletedOk,
                                                            itemToLog.TransactionType,
                                                            _currentTransactionDataStartPosition, contentLength,
                                                            DateTime.UtcNow);
                using(var binaryWriter = new BinaryWriter(stream))
                {
                    transactionHeader.Save(binaryWriter);
                }
            }
        }

        public void LogEndFailedTransaction(ILoggable itemToLog)
        {
            var endPosition = (ulong) _persistenceManager.GetFileLength(GetTransactionLogFile());
            var contentLength = endPosition - _currentTransactionDataStartPosition;

            using (var stream = _persistenceManager.GetOutputStream(GetTransactionLogHeaderFile(), FileMode.Append))
            {
                var transactionHeader = new TransactionInfo(
                    itemToLog.TransactionId, TransactionStatus.Failed,
                    itemToLog.TransactionType,
                    _currentTransactionDataStartPosition,
                    contentLength,
                    DateTime.UtcNow);
                using(var binaryWriter = new BinaryWriter(stream))
                {
                    transactionHeader.Save(binaryWriter);
                }
            }
        }

        public ITransactionInfo ReadTransactionInfo(int transactionNumber)
        {
            if (!_persistenceManager.FileExists(GetTransactionLogHeaderFile()))
            {
                return null;
            }

            using (var stream = _persistenceManager.GetInputStream(GetTransactionLogHeaderFile()))
            {
                var readPosition = transactionNumber * TransactionInfo.TransactionInfoRecordSize;

                // check if this is a valid transaction number, return null if beyond end of file.
                if (readPosition > stream.Length) return null;

                // seek to position
                stream.Seek(-readPosition, SeekOrigin.End);

                // read
                return TransactionInfo.Load(new BinaryReader(stream));
            }
        }

        public Stream GetTransactionData(ulong dataStartPosition)
        {
            if (!_persistenceManager.FileExists(GetTransactionLogFile()))
            {
                return new MemoryStream();
            }

            var stream = _persistenceManager.GetInputStream(GetTransactionLogFile());
            stream.Seek((long)dataStartPosition, SeekOrigin.Begin);
            return stream;
        }

        public TransactionInfoEnumerator GetTransactionList()
        {
            return new TransactionInfoEnumerator(this);
        }

        /// <summary>
        /// Return an enumeration of the most recent transactions in the transaction log in 
        /// time order (oldest first)
        /// </summary>
        /// <param name="maxCount">The maximum number of transactions to return</param>
        /// <param name="ts">The maximum age of transaction to return</param>
        /// <returns>An enumeration of the <paramref name="maxCount"/> most recent transactions that match the age filter</returns>
        public List<ITransactionInfo> GetTransactionList(int maxCount, TimeSpan ts)
        {
            if (!_persistenceManager.FileExists(GetTransactionLogHeaderFile()))
            {
                return new List<ITransactionInfo>();
            }

            using(var stream = _persistenceManager.GetInputStream(GetTransactionLogHeaderFile()))
            {
                long firstRecordOffset = maxCount*TransactionInfo.TransactionInfoRecordSize;
                if (firstRecordOffset > stream.Length)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    stream.Seek(firstRecordOffset, SeekOrigin.End);
                }
                var reader = new BinaryReader(stream);
                var oldestToReturn = DateTime.UtcNow.Subtract(ts);
                var ret = new List<ITransactionInfo>(maxCount);
                while (true)
                {
                    try
                    {
                        var txnInfo = TransactionInfo.Load(reader);
                        if (txnInfo.TransactionStartTime > oldestToReturn)
                        {
                            ret.Add(txnInfo);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
                return ret;
            }
        }

        #endregion

        private string GetTransactionLogFile()
        {
            return Path.Combine(_storeLocation, "transactions.bs");
        }

        private string GetTransactionLogHeaderFile()
        {
            return Path.Combine(_storeLocation, "transactionheaders.bs");
        }
    }
}