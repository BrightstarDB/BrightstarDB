using System;
using System.Text;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class ResourceStore : IResourceStore
    {
        private readonly IResourceTable _resourceTable;

        private const int MaxLocalLiteralLength = 46;
        private const int MaxLocalUriLength = 62;

        public ResourceStore(IResourceTable resourceTable)
        {
            _resourceTable = resourceTable;
        }

        #region Implementation of IResourceStore

        public IResource CreateNew(ulong txnId, string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId, BrightstarProfiler profiler)
        {
            var valueLength = Encoding.UTF8.GetByteCount(resourceValue);
            if (isLiteral)
            {
                return valueLength <= MaxLocalLiteralLength
                           ? new ShortLiteralResource(resourceValue, dataTypeId, langCodeId)
                           : CreateLongLiteralResource(txnId, resourceValue, dataTypeId, langCodeId, profiler);
            }
            return valueLength < MaxLocalUriLength
                       ? new ShortUriResource(resourceValue)
                       : CreateLongUriResource(txnId, resourceValue, profiler);
        }

        public IResource FromBTreeValue(byte[] btreeValue)
        {
            var header = btreeValue[0];
            bool isShort = ((header & (byte) ResourceHeaderFlags.IsShort) == (byte) ResourceHeaderFlags.IsShort);
            bool isLiteral = ((header & (byte) ResourceHeaderFlags.IsLiteral) == (byte) ResourceHeaderFlags.IsLiteral);
            if (isShort)
            {
                if (isLiteral)
                {
                    return new ShortLiteralResource(btreeValue);
                }
                return new ShortUriResource(btreeValue);
            }
            if(isLiteral)
            {
                return new LongLiteralResource(_resourceTable, btreeValue);
            }
            return new LongUriResource(_resourceTable, btreeValue);
        }

        #endregion

        private IResource CreateLongLiteralResource(ulong txnId, string resourceValue, ulong dataTypeId, ulong langCodeId, BrightstarProfiler profiler)
        {
            ulong pageId;
            byte segId;
            _resourceTable.Insert(txnId, resourceValue, out pageId, out segId, profiler);
            return new LongLiteralResource(resourceValue, dataTypeId, langCodeId, pageId, segId);
        }

        private IResource CreateLongUriResource(ulong txnId, string uri, BrightstarProfiler profiler)
        {
            ulong pageId;
            byte segId;
            _resourceTable.Insert(txnId, uri, out pageId, out segId, profiler);
            return new LongUriResource(uri, pageId, segId);
        }
    }
}
