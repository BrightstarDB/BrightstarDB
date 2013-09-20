using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if PORTABLE
using Array = BrightstarDB.Portable.Compatibility.Array;
#endif

namespace BrightstarDB.Storage.Persistence
{
    internal class BinaryPageAdapter : IPage
    {
        private readonly BinaryFilePageStore _store;
        private readonly BinaryFilePage _page;
        private readonly ulong _txnId;
        private bool _isModified;
        private byte[] _data;

        public BinaryPageAdapter(BinaryFilePageStore store, BinaryFilePage page, ulong currentTransactionId, bool isModified)
        {
            _store = store;
            _page = page;
            _txnId = currentTransactionId;
            _isModified = isModified;
            if (_page.FirstTransactionId > currentTransactionId && _page.SecondTransactionId > currentTransactionId)
            {
                // This is an error condition that would happen if a store is kept open while two successive writes are committed
                throw new ReadWriteStoreModifiedException();
            }
            _data = isModified ? _page.GetWriteBuffer(currentTransactionId) : _page.GetReadBuffer(currentTransactionId);
        }

        public ulong Id
        {
            get { return _page.Id; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public bool IsDirty
        {
            get { return _isModified; }
        }

        public long Modified
        {
            get { return 0; }
        }

        public bool Deleted { get; set; }

        public void SetData(byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1)
        {
            _store.EnsureWriteable(_page.Id);
            Array.ConstrainedCopy(data, srcOffset, _data, pageOffset, len < 0 ? data.Length : len);
        }

        public long Write(Stream outputStream, ulong transactionId)
        {
            _page.Write(outputStream, transactionId);
            return 0;
        }

        public long WriteIfModifiedSince(long writeTimestamp, Stream outputStream, ulong transactionId)
        {
            _page.Write(outputStream, transactionId);
            return 0;
        }
    }
}
