using System;
using System.IO;
#if PORTABLE
using Array = BrightstarDB.Portable.Compatibility.Array;
#endif

namespace BrightstarDB.Storage.Persistence
{

    //internal sealed class BinaryPageAdapter : IPage
    //{
    //    private readonly BinaryFilePage _binaryFilePage;

    //    public ulong Id { get; private set; }
    //    public ulong TransactionId { get; private set; }
    //    public byte[] Data { get; private set; }
    //    public bool IsDirty { get; private set; }
    //    public long Modified { get { return 0; } }
    //    public bool Deleted { get; set; }
    //    public bool IsWriteable { get; private set; }

    //    public BinaryPageAdapter(BinaryFilePage page, ulong transactionId, bool isModified, bool isWriteable)
    //    {
    //        _binaryFilePage = page;
    //        TransactionId = transactionId;
    //        Id = page.Id;
    //        Data = page.GetCurrentBuffer(TransactionId);
    //        IsDirty = isModified;
    //        IsWriteable = isWriteable;
    //    }

    //    public void SetData(byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1)
    //    {
    //        AssertWriteable();
    //        Array.ConstrainedCopy(data, srcOffset, Data, pageOffset, len < 0 ? data.Length : len);
    //        IsDirty = true;
    //    }

    //    public long Write(Stream outputStream, ulong transactionId)
    //    {
    //        AssertWriteable();
    //        return _binaryFilePage.Write(outputStream, transactionId);
    //    }

    //    public long WriteIfModifiedSince(long writeTimestamp, Stream outputStream, ulong transactionId)
    //    {
    //        AssertWriteable();
    //        return _binaryFilePage.Write(outputStream, transactionId);
    //    }

    //    private void AssertWriteable()
    //    {
    //        if (!IsWriteable)
    //        {
    //            throw new InvalidOperationException("Attempt to write to a read-only page");
    //        }            
    //    }
    //}

    //internal class __BinaryPageAdapter : IPage
    //{
    //    private readonly BinaryFilePageStore _store;
    //    private readonly BinaryFilePage _page;
    //    private bool _isModified;
    //    private byte[] _data;

    //    public BinaryPageAdapter(BinaryFilePageStore store, BinaryFilePage page, ulong currentTransactionId, bool isModified)
    //    {
    //        _store = store;
    //        _page = page;
    //        _isModified = isModified;
    //        if (_page.FirstTransactionId > currentTransactionId && _page.SecondTransactionId > currentTransactionId)
    //        {
    //            // This is an error condition that would happen if a store is kept open while two successive writes are committed
    //            throw new ReadWriteStoreModifiedException();
    //        }
    //        _data = isModified ? _page.GetWriteBuffer(currentTransactionId) : _page.GetReadBuffer(currentTransactionId);
    //    }

    //    public ulong Id
    //    {
    //        get { return _page.Id; }
    //    }

    //    public byte[] Data
    //    {
    //        get { return _data; }
    //    }

    //    public bool IsDirty
    //    {
    //        get { return _isModified; }
    //    }

    //    public long Modified
    //    {
    //        get { return 0; }
    //    }

    //    public bool Deleted { get; set; }

    //    public void SetData(byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1)
    //    {
    //        _store.EnsureWriteable(_page.Id);
    //        Array.ConstrainedCopy(data, srcOffset, _data, pageOffset, len < 0 ? data.Length : len);
    //    }

    //    public long Write(Stream outputStream, ulong transactionId)
    //    {
    //        _page.Write(outputStream, transactionId);
    //        return 0;
    //    }

    //    public long WriteIfModifiedSince(long writeTimestamp, Stream outputStream, ulong transactionId)
    //    {
    //        _page.Write(outputStream, transactionId);
    //        return 0;
    //    }
    //}
}
