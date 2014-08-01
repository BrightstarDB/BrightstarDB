using System;
using System.IO;
using System.Threading;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#elif WINDOWS_PHONE
using BrightstarDB.Mobile.Compatibility;
#else
using System.Collections.Concurrent;
#endif

namespace BrightstarDB.Storage.Persistence
{
    internal class BackgroundPageWriter : IDisposable
    {
        private readonly ConcurrentQueue<ulong> _writeQueue; 
        private readonly ConcurrentDictionary<ulong, WriteTask> _writeTasks;
        private bool _shutdownRequested;
        private readonly ConcurrentDictionary<ulong, long> _writeTimestamps;
        private readonly Stream _outputStream;
        private readonly ManualResetEvent _shutdownCompleted;
        private WriteTask _writing;

        public BackgroundPageWriter(Stream outputStream)
        {
            _outputStream = outputStream;
            _writeQueue = new ConcurrentQueue<ulong>();
            _writeTasks = new ConcurrentDictionary<ulong, WriteTask>();
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
            var writeTask = new WriteTask {PageToWrite = pageToWrite, TransactionId = transactionId};
            _writeTasks.AddOrUpdate(pageToWrite.Id, writeTask, (pageId, task) => writeTask);
            _writeQueue.Enqueue(pageToWrite.Id);
        }

        public void Flush()
        {
            while(!_writeQueue.IsEmpty)
            {
                _shutdownCompleted.WaitOne(10); // Spin until the queue is empty
            }
            _outputStream.Flush();
        }

        public bool TryGetPage(ulong pageId, out IPage page)
        {
            WriteTask writeTask;
            if (_writeTasks.TryGetValue(pageId, out writeTask))
            {
                page = writeTask.PageToWrite;
                return true;
            }
            lock (this)
            {
                if (_writing != null && _writing.PageToWrite.Id == pageId)
                {
                    page = _writing.PageToWrite;
                    return true;
                }
            }
            page = null;
            return false;
        }

        private void Run(object state)
        {
            while (!_shutdownRequested)
            {
                ulong writePageId;
                if (_writeQueue.TryDequeue(out writePageId))
                {
#if DEBUG_PAGESTORE
                    Logging.LogDebug("BackgroundWriter: Next page id in queue: {0}", writePageId);
#endif
                    // Retrieve the page write information from the _writeTasks dictionary
                    WriteTask writeTask;
                    if (_writeTasks.TryRemove(writePageId, out writeTask))
                    {
                        lock (this)
                        {
                            _writing = writeTask;
                        }
#if DEBUG_PAGESTORE
                    Logging.LogDebug("BackgroundWriter: Page {0} found in task dictionary.", writePageId);
#endif
                        try
                        {
                            _writing.PageToWrite.Write(_outputStream, _writing.TransactionId);
                        }
                        catch (Exception ex)
                        {
                            Logging.LogError(BrightstarEventId.StoreBackgroundWriteError,
                                "Error in BackgroundPageWriter: {0}", ex);
                        }
                        finally
                        {
                            lock (this)
                            {
                                _writing = null;
                            }
                        }
                    }
                    else
                    {
#if DEBUG_PAGESTORE
                        Logging.LogDebug("BackgroundWriter: Page {0} no longer in task dictionary. Skipping.", writePageId);
#endif
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
