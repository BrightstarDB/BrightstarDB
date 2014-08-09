using System;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class DuplicateKeyException : Exception
    {
        public ulong PageId { get; private set; }

        public DuplicateKeyException(ulong pageId) : base("Encountered a duplicate key exception while inserting into page#" + pageId)
        {
            PageId = pageId;
        }
    }
}