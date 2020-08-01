using System;

namespace BrightstarDB.Storage.Persistence
{
    internal class EvictionEventArgs : EventArgs
    {
        public string Partition { get; private set; }
        public ulong PageId { get; private set; }
        public bool CancelEviction { get; set; }

        public EvictionEventArgs(string partition, ulong pageId)
        {
            Partition = partition;
            PageId = pageId;
            CancelEviction = false;
        }
    }
}