using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#else
using System.Collections.Concurrent;
#endif

namespace BrightstarDB.Storage.Persistence
{
    internal class BackgroundPageWriter : IDisposable
    {
        private readonly ConcurrentQueue<WriteTask> _writeTasks;
        private bool _shutdownRequested;
        private readonly ConcurrentDictionary<ulong, long> _writeTimestamps;
        private readonly Stream _outputStream;
        private readonly ManualResetEvent _shutdownCompleted;

        public BackgroundPageWriter(Stream outputStream)
        {
            _outputStream = outputStream;
            _writeTasks = new ConcurrentQueue<WriteTask>();
            _shutdownRequested = false;
            _shutdownCompleted = new ManualResetEvent(false);
            _writeTimestamps = new ConcurrentDictionary<ulong, long>();
            ThreadPool.QueueUserWorkItem(Run);
        }

        public void Shutdown()
        {
            _shutdownRequested = true;
            _shutdownCompleted.WaitOne();
        }

        public void QueueWrite(IPage pageToWrite, ulong transactionId)
        {
#if DEBUG_PAGESTORE
            Logging.LogDebug("Queue {0}", pageToWrite.Id);
#endif
            _writeTasks.Enqueue(new WriteTask{PageToWrite = pageToWrite, TransactionId = transactionId});
        }

        public void Flush()
        {
            while(!_writeTasks.IsEmpty)
            {
                _shutdownCompleted.WaitOne(10); // Spin until the queue is empty
            }
            _outputStream.Flush();
        }

        private void Run(object state)
        {
            while (!_shutdownRequested)
            {
                WriteTask writeTask;
                if (_writeTasks.TryDequeue(out writeTask))
                {
                    try
                    {
                        long writeTimestamp;
                        _writeTimestamps.TryGetValue(writeTask.PageToWrite.Id, out writeTimestamp);
                        _writeTimestamps[writeTask.PageToWrite.Id] =
                            writeTask.PageToWrite.WriteIfModifiedSince(writeTimestamp, _outputStream,
                                                                       writeTask.TransactionId);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(BrightstarEventId.StoreBackgroundWriteError, "Error in BackgroundPageWriter: {0}", ex);
                    }
                }
                else
                {
                    // Instead of waiting on a event, just spin wait
                    _shutdownCompleted.WaitOne(1);
                }
            }
            _shutdownCompleted.Set();
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
                _outputStream.Flush();
                _outputStream.Close();
            }
        }

        ~BackgroundPageWriter()
        {
            Dispose(false);
        }

        public void ResetTimestamp(ulong pageId)
        {
            _writeTimestamps[pageId] = 0;
        }
    }

    internal class WriteTask
    {
        public IPage PageToWrite { get; set; }
        public ulong TransactionId { get; set; }
    }
}
