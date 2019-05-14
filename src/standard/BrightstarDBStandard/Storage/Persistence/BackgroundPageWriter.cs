using System;
using System.Collections.Generic;
using System.IO;
#if PORTABLE
using System.Linq;
#endif

namespace BrightstarDB.Storage.Persistence
{
    internal class BackgroundPageWriter : IDisposable
    {
        private readonly Stream _outputStream;
#if PORTABLE
        private readonly Dictionary<ulong, WriteTask> _pages;
#else
        private readonly SortedList<ulong, WriteTask> _pages;
#endif

        public BackgroundPageWriter(Stream outputStream, int capacity)
        {
            _outputStream = outputStream;
#if PORTABLE
            _pages = new Dictionary<ulong, WriteTask>(capacity);
#else
            _pages = new SortedList<ulong, WriteTask>(capacity);
#endif
        }

        public void QueueWrite(IPage pageToWrite, ulong txnId)
        {
            _pages[pageToWrite.Id] = new WriteTask(pageToWrite, txnId);
        }

        public void Flush()
        {
#if PORTABLE
            foreach (var writeTask in _pages.Keys.OrderBy(x => x).Select(pid => _pages[pid]))
            {
                writeTask.PageToWrite.Write(_outputStream, writeTask.TransactionId);
            }
#else
            foreach (var page in _pages.Values)
            {
                page.PageToWrite.Write(_outputStream, page.TransactionId);
            }
#endif
            _outputStream.Flush();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
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
        public WriteTask(IPage pageToWrite, ulong txnId)
        {
            PageToWrite = pageToWrite;
            TransactionId = txnId;
        }

        public IPage PageToWrite { get; private set; }
        public ulong TransactionId { get; private set; }
    }
}
