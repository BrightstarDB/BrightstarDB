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
        private bool _stopRequested;
        //private readonly AutoResetEvent _taskAdded;
        private readonly Dictionary<ulong, long> _writeTimestamps;
        private readonly Thread _writerThread;
        private readonly Stream _outputStream;

        public BackgroundPageWriter(Stream outputStream)
        {
            _outputStream = outputStream;
            _writeTasks = new ConcurrentQueue<WriteTask>();
            _shutdownRequested = false;
            _stopRequested = false;
            //_taskAdded = new AutoResetEvent(false);
            _writeTimestamps = new Dictionary<ulong, long>();
            _writerThread = new Thread(Run);
            _writerThread.Start();
        }

        public void Shutdown()
        {
            if (_writerThread.IsAlive)
            {
                //_taskAdded.Set();
                _shutdownRequested = true;
                _writerThread.Join();
            }
        }

        public void Stop()
        {
            if (_writerThread.IsAlive)
            {
                _stopRequested = true;
                _writerThread.Join();
            }
        }

        public void QueueWrite(IPage pageToWrite, ulong transactionId)
        {
#if DEBUG_PAGESTORE
            Logging.LogDebug("Queue {0}", pageToWrite.Id);
#endif
            _writeTasks.Enqueue(new WriteTask{PageToWrite = pageToWrite, TransactionId = transactionId});
            //_taskAdded.Set();
        }

        public void Flush()
        {
            while(!_writeTasks.IsEmpty)
            {
                Thread.Sleep(10); // Spin until the queue is empty
            }
            _outputStream.Flush();
        }

        public void Run()
        {
            while (!_shutdownRequested)
            {
                WriteTask writeTask;
                if (_writeTasks.TryDequeue(out writeTask))
                {
                    /*
                    long writeTimestamp;
                    if (_writeTimestamps.TryGetValue(writeTask.PageToWrite.Id, out writeTimestamp) &&
                        writeTimestamp >= writeTask.PageToWrite.Modified)
                    {
                        // Page already written
                        //Logging.LogInfo("Page {0} already written at timestamp {1}. Skipping write", writeTask.PageToWrite.Id, writeTask.PageToWrite.Modified);
                        continue;
                    }
                    writeTimestamp = writeTask.PageToWrite.Write(_outputStream, writeTask.TransactionId);
                    _writeTimestamps[writeTask.PageToWrite.Id] = writeTimestamp;
                    //Logging.LogInfo("Background write of page {0} @ {1} completed.", writeTask.PageToWrite.Id,
                    //                writeTimestamp);
                     */
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
                    if (_writeTasks.IsEmpty && _stopRequested)
                    {
                        Logging.LogInfo("Stop requested and no further pages left to write.");
                        return;
                    }
                    //_taskAdded.WaitOne(3000);
                    // Instead of waiting on a event, just spin wait
                    Thread.Sleep(0);
                }
            }
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
