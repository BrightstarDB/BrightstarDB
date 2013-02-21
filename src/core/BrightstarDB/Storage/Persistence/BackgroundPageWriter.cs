using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BrightstarDB.Storage.Persistence
{
    internal class BackgroundPageWriter : IDisposable
    {
        private readonly ConcurrentQueue<WriteTask> _writeTasks;
        private bool _shutdownRequested;
        private bool _stopRequested;
        private readonly AutoResetEvent _taskAdded;
        private readonly Dictionary<ulong, long> _writeTimestamps;
        private readonly Thread _writerThread;
        private readonly Stream _outputStream;

        public BackgroundPageWriter(Stream outputStream)
        {
            _outputStream = outputStream;
            _writeTasks = new ConcurrentQueue<WriteTask>();
            _shutdownRequested = false;
            _stopRequested = false;
            _taskAdded = new AutoResetEvent(false);
            _writeTimestamps = new Dictionary<ulong, long>();
            _writerThread = new Thread(Run);
            _writerThread.Start();
        }

        public void Shutdown()
        {
            if (_writerThread.IsAlive)
            {
                _taskAdded.Set();
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
            _writeTasks.Enqueue(new WriteTask{PageToWrite = pageToWrite, TransactionId = transactionId});
            _taskAdded.Set();
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
                    long writeTimestamp;
                    _writeTimestamps.TryGetValue(writeTask.PageToWrite.Id, out writeTimestamp);
                    _writeTimestamps[writeTask.PageToWrite.Id] =
                        writeTask.PageToWrite.WriteIfModifiedSince(writeTimestamp, _outputStream,
                                                                   writeTask.TransactionId);
                }
                else
                {
                    if (_writeTasks.IsEmpty && _stopRequested)
                    {
                        Logging.LogInfo("Stop requested and no further pages left to write.");
                        return;
                    }
                    _taskAdded.WaitOne(3000);
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
    }

    internal class WriteTask
    {
        public IPage PageToWrite { get; set; }
        public ulong TransactionId { get; set; }
    }
}
