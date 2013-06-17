using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage.Persistence
{
    internal class BinaryPageAdapter : IPage
    {
        private readonly BinaryFilePageStore _store;
        private readonly BinaryFilePage _page;
        private readonly ulong _txnId;
        private long _modified;
        
        public BinaryPageAdapter(BinaryFilePageStore store, BinaryFilePage page, ulong currentTransactionId, bool isModified)
        {
            _store = store;
            _page = page;
            _txnId = currentTransactionId;
            _modified = isModified ? 1 : 0;
            if (_page.FirstTransactionId > currentTransactionId && _page.SecondTransactionId > currentTransactionId)
            {
                // This is an error condition that would happen if a store is kept open while two successive writes are committed
                throw new ReadWriteStoreModifiedException();
            }
        }

        public BinaryPageAdapter(BinaryFilePageStore store, BinaryFilePage page, ulong currentTransactionId, ulong nextCommitId, bool isModified)
        {
            _store = store;
            _page = page;
            _txnId = currentTransactionId;
            WriteTransactionId = nextCommitId;
            if (_page.FirstTransactionId > currentTransactionId && _page.SecondTransactionId > currentTransactionId)
            {
                // This is an error condition that would happen if a store is kept open while two successive writes are committed
                throw new ReadWriteStoreModifiedException();
            }
            _modified = isModified ? 1 : 0;
        }

        public ulong WriteTransactionId { get; private set; }

        public ulong Id
        {
            get { return _page.Id; }
        }

        public byte[] Data
        {
            get { return IsDirty ? _page.GetWriteBuffer(WriteTransactionId) : _page.GetReadBuffer(_txnId); }
        }

        public bool IsDirty
        {
            get { return _modified > 0; }
        }

        public long Modified
        {
            get { return _modified; }
        }

        public bool Deleted { get; set; }

        public void SetData(byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1)
        {
            if (WriteTransactionId == 0) throw new InvalidOperationException(Strings.INode_Attempt_to_write_to_a_fixed_page);
            Array.ConstrainedCopy(data, srcOffset, _page.GetWriteBuffer(WriteTransactionId), pageOffset, len < 0 ? data.Length : len);
            _store.MarkDirty(_page);
            _modified++;
        }

        public long Write(Stream outputStream, ulong transactionId)
        {
            _page.Write(outputStream, transactionId);
            return _modified;
        }

        public long WriteIfModifiedSince(long writeTimestamp, Stream outputStream, ulong transactionId)
        {
            long ret = _modified;
            if (_modified > writeTimestamp)
            {
                _page.Write(outputStream, transactionId);

            }
            return ret;
        }

        public void MakeWriteable(ulong writeTransactionId)
        {
            var srcData = _page.GetReadBuffer(_txnId);
            Array.Copy(srcData, _page.GetWriteBuffer(writeTransactionId), srcData.Length);
            WriteTransactionId = writeTransactionId;
            _modified++;
        }
    }
}
